global using NUnit.Framework;
global using Jitter2;
global using Jitter2.Collision;
global using Jitter2.Dynamics;
global using Jitter2.LinearMath;
global using Jitter2.Collision.Shapes;

#if USE_DOUBLE_PRECISION

global using Real = System.Double;
global using MathR = System.Math;

#else

global using Real = System.Single;
global using MathR = System.MathF;

#endif
