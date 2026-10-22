using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer;

/// Phong lit shader consumed by <see cref="InstancedDrawable"/> for the lit pass.
/// Samples <see cref="ShadowCaster.CascadeCount"/> shadow maps bound to texture
/// units 0..CASCADES-1 and a diffuse texture bound to unit 3.
public static class LitShader
{
    public static Shader Create()
    {
        var shader = new Shader(Vs, Fs);
        shader.Use();

        // Bind the samplers to the fixed texture units matched by ShadowCaster / InstancedDrawable.
        shader.Set("uShadowNear", 0);
        shader.Set("uShadowMid", 1);
        shader.Set("uShadowFar", 2);
        shader.Set("uDiffuse", 3);

        return shader;
    }

    // Vertex: 8-float vertex stream (pos, normal, uv) + per-instance mat4 + color.
    // The normal is transformed by the inverse-transpose of the instance matrix so
    // that non-uniform scale (which the demo uses heavily) does not warp shading.
    private const string Vs = @"
#version 330 core

layout(location = 0) in vec3 aPos;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aTexCoord;
layout(location = 3) in mat4 aInstanceModel;
layout(location = 7) in vec3 aInstanceColor;

uniform mat4 uView;
uniform mat4 uProjection;

out vec3 vWorldPos;
out vec3 vWorldNormal;
out vec2 vTexCoord;
out vec3 vInstanceColor;

void main()
{
    vec4 wp = aInstanceModel * vec4(aPos, 1.0);
    vWorldPos = wp.xyz;
    vWorldNormal = normalize(mat3(transpose(inverse(aInstanceModel))) * aNormal);
    vTexCoord = vec2(aTexCoord.x, 1.0 - aTexCoord.y);
    vInstanceColor = aInstanceColor;
    gl_Position = uProjection * uView * wp;
}
";

    // Fragment: material struct matches fields set by InstancedDrawable.ApplyMaterial.
    // Cascade count and split logic are hard-compiled to 3 for simplicity; change
    // both the GLSL and ShadowCaster.CascadeCount together if you need more.
    private const string Fs = @"
#version 330 core

struct Material {
    vec3  tint;
    vec3  specular;
    float shininess;
    float alpha;
    float vertexColorWeight;
    float textureWeight;
    float normalFlip;
};

uniform Material uMat;
uniform vec3 uViewPos;
uniform mat4 uView;
uniform vec3 uSunDir;

uniform mat4 uLightMatrices[3];
uniform float uCascadeSplits[3];

uniform sampler2D uShadowNear;
uniform sampler2D uShadowMid;
uniform sampler2D uShadowFar;
uniform sampler2D uDiffuse;

in vec3 vWorldPos;
in vec3 vWorldNormal;
in vec2 vTexCoord;
in vec3 vInstanceColor;

out vec4 FragColor;

float sampleShadow(mat4 lightMatrix, sampler2D shadowmap, vec3 n)
{
    // Slope-scale bias. The 0.6/0.4 coefficients are empirical: they widen the
    // bias on surfaces nearly parallel to the sun (to kill shadow acne) while
    // keeping it near-zero on surfaces facing the sun head-on (to avoid Peter
    // Panning). If cascades gain resolution or the light angle changes,
    // re-tune by eye.
    float bias = max(0.6 * (0.4 - dot(n, uSunDir)), 0.0001);
    vec4 lp = lightMatrix * vec4(vWorldPos, 1.0);
    vec3 proj = lp.xyz / lp.w * 0.5 + 0.5;

    if (proj.z > 1.0) return 0.0;

    float mapped = texture(shadowmap, proj.xy).r;
    if (proj.z - bias > mapped)
    {
        // Empirical: return abs(dot(n, sun)) as the shadow strength instead of a
        // flat 1.0. Combined with `(diffusive + specular) * (1 - shadow)` in main,
        // this cancels roughly the sun's own diffuse contribution on shadowed
        // fragments while leaving the three fill lights mostly intact — looks
        // softer than a hard binary shadow.
        return abs(dot(n, uSunDir));
    }
    return 0.0;
}

float shadowForPixel(vec3 n)
{
    float viewDepth = abs((uView * vec4(vWorldPos, 1.0)).z);
    if (viewDepth < uCascadeSplits[0]) return sampleShadow(uLightMatrices[0], uShadowNear, n);
    if (viewDepth < uCascadeSplits[1]) return sampleShadow(uLightMatrices[1], uShadowMid, n);
    return sampleShadow(uLightMatrices[2], uShadowFar, n);
}

void main()
{
    vec3 n = normalize(vWorldNormal) * uMat.normalFlip;

    vec3 ambient = uMat.vertexColorWeight * vInstanceColor + (1.0 - uMat.vertexColorWeight) * uMat.tint;
    vec3 diffuseBase = mix(vec3(0.6), texture(uDiffuse, vTexCoord).rgb, uMat.textureWeight);

    // Four fixed directional fills: main sun plus three rim/bounce.
    vec3 lightDirs[4];
    float lightStrengths[4];
    lightDirs[0] = uSunDir;          lightStrengths[0] = 1.0;
    lightDirs[1] = vec3(-1, 0,  1);  lightStrengths[1] = 0.2;
    lightDirs[2] = vec3( 0, 0, -1);  lightStrengths[2] = 0.2;
    lightDirs[3] = vec3(-1, 0, -1);  lightStrengths[3] = 0.2;

    vec3 diffusive = vec3(0);
    for (int i = 0; i < 4; i++)
    {
        vec3 ld = normalize(lightDirs[i]);
        float d = max(dot(n, ld), 0.0);
        diffusive += lightStrengths[i] * d * diffuseBase;
    }

    vec3 viewDir = normalize(uViewPos - vWorldPos);
    vec3 halfway = normalize(normalize(lightDirs[0]) + viewDir);
    float specFactor = pow(max(dot(n, halfway), 0.0), uMat.shininess);
    vec3 specular = specFactor * uMat.specular;

    float shadow = shadowForPixel(n);
    vec3 color = ambient + (diffusive + 0.4 * specular) * (1.0 - shadow);

    FragColor = vec4(color, uMat.alpha);
}
";
}
