/* Copyright <2022> <Thorben Linneweber>
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

using JitterDemo.Renderer.OpenGL.Native;

namespace JitterDemo.Renderer.OpenGL;

public enum GLDebugMessageSeverity : uint
{
    Notification = GLC.DEBUG_SEVERITY_NOTIFICATION,
    Low = GLC.DEBUG_SEVERITY_LOW,
    Medium = GLC.DEBUG_SEVERITY_MEDIUM,
    High = GLC.DEBUG_SEVERITY_HIGH
}

public enum GLDebugMessageSource : uint
{
    API = GLC.DEBUG_SOURCE_API,
    WindowSystem = GLC.DEBUG_SOURCE_WINDOW_SYSTEM,
    ShaderCompiler = GLC.DEBUG_SOURCE_SHADER_COMPILER,
    ThirdParty = GLC.DEBUG_SOURCE_THIRD_PARTY,
    Application = GLC.DEBUG_SOURCE_APPLICATION,
    Other = GLC.DEBUG_SOURCE_OTHER
}

public enum GLDebugMessageType : uint
{
    Error = GLC.DEBUG_TYPE_ERROR,
    DeprecatedBehavior = GLC.DEBUG_TYPE_DEPRECATED_BEHAVIOR,
    UndefinedBehavior = GLC.DEBUG_TYPE_UNDEFINED_BEHAVIOR,
    Portability = GLC.DEBUG_TYPE_PORTABILITY,
    Performance = GLC.DEBUG_TYPE_PERFORMANCE,
    Marker = GLC.DEBUG_TYPE_MARKER,
    PushGroup = GLC.DEBUG_TYPE_PUSH_GROUP,
    PopGroup = GLC.DEBUG_TYPE_POP_GROUP,
    Other = GLC.DEBUG_TYPE_OTHER
}

public struct GLDebugMessage
{
    public GLDebugMessageSeverity Severity;
    public GLDebugMessageSource Source;
    public GLDebugMessageType Type;
    public string Message;
    public uint Id;
}