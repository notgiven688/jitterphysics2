#if USE_DOUBLE_PRECISION
global using Real = System.Double;
global using MathR = System.Math;
#else
global using Real = System.Single;
global using MathR = System.MathF;
#endif