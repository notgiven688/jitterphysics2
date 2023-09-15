/* Copyright <2021> <Thorben Linneweber>
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 *
 */

using System;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer;

public class Camera
{
    public bool IgnoreMouseInput { get; set; } = false;
    public bool IgnoreKeyboardInput { get; set; } = false;
    public Matrix4 ViewMatrix { get; protected set; } = Matrix4.Identity;
    public Matrix4 ProjectionMatrix { get; protected set; } = Matrix4.Identity;

    public Vector3 Position { get; set; }
    public Vector3 Direction { get; protected set; }

    public float FieldOfView { get; set; } = MathF.PI / 4.0f;

    public double Theta { get; set; } = Math.PI / 2.0d;
    public double Phi { get; set; }

    public float NearPlane { get; protected set; } = 0.1f;
    public float FarPlane { get; protected set; } = 400.0f;

    public virtual void Update()
    {
    }
}

public class FreeCamera : Camera
{
    private const float MoveSpeed = 0.4f;
    private const float MouseSensitivity = 0.006f;

    public override void Update()
    {
        Keyboard kb = Keyboard.Instance;
        Mouse ms = Mouse.Instance;

        if (!IgnoreMouseInput && ms.IsButtonDown(Mouse.Button.Right))
        {
            Phi -= ms.DeltaPosition.X * MouseSensitivity;
            Theta += ms.DeltaPosition.Y * MouseSensitivity;
        }

        if (Theta > Math.PI - 0.1d) Theta = Math.PI - 0.1d;
        if (Theta < 0.1d) Theta = 0.1d;

        Direction = new Vector3
        {
            Z = -(float)(Math.Sin(Theta) * Math.Cos(Phi)),
            X = -(float)(Math.Sin(Theta) * Math.Sin(Phi)),
            Y = (float)Math.Cos(Theta)
        };

        Vector3 cright = Vector3.Normalize(Vector3.UnitY % Direction);
        Vector3 mv = Vector3.Zero;

        if (!IgnoreKeyboardInput && !kb.IsKeyDown(Keyboard.Key.LeftControl))
        {
            if (kb.IsKeyDown(Keyboard.Key.W)) mv += Direction;
            if (kb.IsKeyDown(Keyboard.Key.S)) mv -= Direction;
            if (kb.IsKeyDown(Keyboard.Key.A)) mv += cright;
            if (kb.IsKeyDown(Keyboard.Key.D)) mv -= cright;
        }

        if (mv.LengthSquared() > 0.1f) mv = Vector3.Normalize(mv);
        Position += MoveSpeed * mv;

        float width = RenderWindow.Instance.Width;
        float height = RenderWindow.Instance.Height;

        ViewMatrix = MatrixHelper.CreateLookAt(Position, Position + Direction, Vector3.UnitY);
        ProjectionMatrix =
            MatrixHelper.CreatePerspectiveFieldOfView(FieldOfView, width / height, NearPlane, FarPlane);
    }
}