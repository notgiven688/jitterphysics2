using System;
using System.Runtime.InteropServices.JavaScript;
using Raylib_cs;
using static Raylib_cs.Raylib;

Console.WriteLine("Hello, Browser!");

// Initialization
//--------------------------------------------------------------------------------------
const int screenWidth = 800;
const int screenHeight = 450;


unsafe
{
    InitWindow(screenWidth, screenHeight, (sbyte*)(0));
}


// Main game loop
while (!WindowShouldClose())    // Detect window close button or ESC key
{
    // Update
    //----------------------------------------------------------------------------------
    // TODO: Update your variables here
    //----------------------------------------------------------------------------------

    // Draw
    //----------------------------------------------------------------------------------
    BeginDrawing();

    ClearBackground(Color.RAYWHITE);

    DrawText("Congrats! You created your first window!", 190, 200, 20, Color.LIGHTGRAY);

    EndDrawing();
    //----------------------------------------------------------------------------------
}


CloseWindow();

return 0;

public partial class MyClass
{
    [JSExport]
    internal static string Greeting()
    {
        var text = $"Hello, World! Greetings from {GetHRef()}";
        Console.WriteLine(text);
        return text;
    }

    [JSImport("window.location.href", "main.js")]
    internal static partial string GetHRef();
    
}
