using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer;

public class ShadowShader : BasicShader
{
    public UniformMatrix4 ProjectionView { private set; get; }
    public UniformMatrix4 Model { private set; get; }

    public ShadowShader() : base(vshader, fshader)
    {
        ProjectionView = GetUniform<UniformMatrix4>("projectionview");
        Model = GetUniform<UniformMatrix4>("model");
    }

    private static readonly string vshader = @"
        #version 330 core
        layout (location = 0) in mat4 aModel;
        layout (location = 5) in vec3 aPos;

        uniform mat4 projectionview;
        uniform mat4 model;

        void main()
        {
            gl_Position = projectionview * aModel * model * vec4(aPos, 1.0);
        }  
        ";

    private static readonly string fshader = @"
        #version 330 core
        out vec4 FragColor;
        void main()
        {             
        } 
        ";
}

public class PhongShader : BasicShader
{
    public class Material
    {
        public UniformVector3 Color { get; }
        public UniformVector3 Specular { get; }
        public UniformFloat Shininess { get; }
        public UniformFloat Alpha { get; }

        private UniformFloat NormalMultiply { get; }

        public bool FlipNormal
        {
            set => NormalMultiply.Set(value ? -1.0f : 1.0f);
        }

        /// <summary>
        /// Magic:
        /// Ambient = ColorMixing.X * vertexColor + ColorMixing.Y * shaderColor
        /// Diffusive = (1.0f - ColorMixing.Z) * vec3(0.6f) +  ColorMixing.Z * textureColor.
        /// </summary>
        public UniformVector3 ColorMixing { get; }

        public Material(ShaderProgram shader)
        {
            Color = shader.GetUniform<UniformVector3>("material.color");
            Specular = shader.GetUniform<UniformVector3>("material.specular");
            Shininess = shader.GetUniform<UniformFloat>("material.shininess");
            Alpha = shader.GetUniform<UniformFloat>("material.alpha");
            ColorMixing = shader.GetUniform<UniformVector3>("material.mixing");
            NormalMultiply = shader.GetUniform<UniformFloat>("material.flipnormal");
        }

        public void SetDefaultMaterial()
        {
            Color.Set(0.0f, 0.0f, 0.0f);
            Specular.Set(0.1f, 0.1f, 0.1f);
            Shininess.Set(128);
            Alpha.Set(1.0f);
            ColorMixing.Set(1, 0, 0);
            NormalMultiply.Set(1);
        }
    }

    public UniformMatrix4 View { private set; get; }
    public UniformMatrix4 Projection { private set; get; }
    public UniformVector3 ViewPosition { private set; get; }
    public UniformVector3 SunDir { private set; get; }
    public Material MaterialProperties { private set; get; }
    public UniformMatrix4 Lights { private set; get; }
    public UniformTexture DiffuseTexture { private set; get; }
    public UniformMatrix4 Model { private set; get; }

    public PhongShader() : base(vshader, fshader)
    {
        View = GetUniform<UniformMatrix4>("view");
        Projection = GetUniform<UniformMatrix4>("projection");
        ViewPosition = GetUniform<UniformVector3>("viewPos");
        Lights = GetUniform<UniformMatrix4>("lightmaps[0]");
        DiffuseTexture = GetUniform<UniformTexture>("diffuse");
        SunDir = GetUniform<UniformVector3>("sundir");
        Model = GetUniform<UniformMatrix4>("model");

        MaterialProperties = new Material(this);
    }

    private static readonly string vshader = @"
        #version 330 core

        layout (location = 0) in mat4 amodel;
        layout (location = 4) in vec3 acolor;
        layout (location = 5) in vec3 aPos;
        layout (location = 6) in vec3 aNorm;
        layout (location = 7) in vec2 aTexCoords;
        
        uniform mat4 view;
        uniform mat4 projection;
        uniform mat4[3] lightmaps;
        uniform mat4 model;

        out vec3 vertexColor;
        out vec3 normal;
        out vec3 pos;
        out vec2 TexCoords;

        void main()
        {
            mat4 fmodel = amodel * model;
            gl_Position = projection * view * fmodel * vec4(aPos, 1.0);
            normal = normalize(mat3(transpose(inverse(fmodel))) * normalize(aNorm));
            
            pos = vec3(fmodel * vec4(aPos, 1.0));
            TexCoords.y = 1-aTexCoords.y;
            TexCoords.x = aTexCoords.x;

            vertexColor = acolor;
        }
        ";

    private static readonly string fshader = @"
        #version 420 core
        struct Material {
            vec3 color;
            vec3 specular;
            float shininess;
            vec3 diffuse;
            float alpha;
            vec3 mixing;
            float flipnormal;
        };

        uniform Material material;
        uniform vec3 viewPos;
        uniform mat4 view;
        uniform vec3 sundir;

        uniform mat4[3] lightmaps;

        layout(binding=0)
        uniform sampler2D shadowNear;
        layout(binding=1)
        uniform sampler2D shadowMid;
        layout(binding=2)
        uniform sampler2D shadowFar;

        layout(binding=3)
        uniform sampler2D diffuse;
    
        in vec3 normal;
        in vec3 pos;
        in vec2 TexCoords;
        in vec3 ambient;
        in vec3 vertexColor;

        out vec4 FragColor;

        float calcShadow(mat4 light, sampler2D shadowmap, vec3 nn)
        {
            // (*) Empirically found to look okay.
            float bias = max(0.6 * (0.4 - dot(nn, sundir)), 0.0001);

            vec4 fragPosLightSpace = light * vec4(pos, 1.0);
            vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
            projCoords = projCoords * 0.5 + 0.5; 

            float currentDepth = projCoords.z;  

            if(projCoords.z > 1.0) return 0.0;

            float pcfDepth = texture(shadowmap, projCoords.xy).r; 
            if (currentDepth - bias > pcfDepth)
            {
                // (*) same here.
                return abs(dot(nn, sundir));
            }
            return 0.0;
        }

        float ShadowCalculation(vec3 nn)
        {
            float shadow;

            vec4 fragPosViewSpace = view * vec4(pos, 1.0);
            float depthvalue = abs(fragPosViewSpace.z);

            if(depthvalue < 20)
            {
                shadow = calcShadow(lightmaps[0], shadowNear, nn);
            }
            else if (depthvalue < 60)
            {
                shadow = calcShadow(lightmaps[1], shadowMid, nn);
            }
            else
            {
                shadow = calcShadow(lightmaps[2], shadowFar, nn);
            }

            return shadow;
        }
      
        void main()
        {
            vec3 lightColor = vec3(1, 1, 1);
            vec3 mix = material.mixing;
            vec3 fnormal = normal * material.flipnormal;

            vec3 ambient = mix.x * vertexColor + mix.y * material.color;
            vec3 diffuse = (1.0f - mix.z) * vec3(0.6f, 0.6f, 0.6f) + mix.z * vec3(texture(diffuse, TexCoords));

            vec3 lights[4];
            float lightstrength[4];

            lightstrength[0] = 1.0f;
            lightstrength[1] = 0.2f;
            lightstrength[2] = 0.2f;
            lightstrength[3] = 0.2f;

            lights[0] = sundir;  // main light from the sun
            lights[1] = vec3(-1, 0, 1);
            lights[2] = vec3(0, 0, -1);
            lights[3] = vec3(-1, 0, -1);

            vec3 diffusive = vec3(0, 0, 0);
            vec3 specular = vec3(0, 0, 0);
            vec3 lightDir;

            for(int i=0; i<4; i++)
            {
                // diffuse
                lightDir = normalize(lights[i]);
                float diff = max(dot(fnormal, lightDir), 0.0);
                diffusive += lightColor * lightstrength[i] * diff * diffuse;
            }

            // specular
            lightDir = normalize(lights[0]);
            vec3 viewDir = normalize(viewPos - pos);
            vec3 halfwayDir = normalize(lightDir + viewDir);
            float spec = pow(max(dot(viewDir, halfwayDir),0.0), material.shininess);
            specular = lightColor * spec * material.specular;
            
            float shadow = ShadowCalculation(fnormal);
            
            vec3 result = (1.0f*ambient + 1.0f*(diffusive + 0.4f*specular) * (1- shadow));
            FragColor = vec4(result, material.alpha);
        }
        ";
}