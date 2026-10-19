using Jitter2;
using Vellum;

namespace JitterDemo;

public interface ICleanDemo
{
    public void CleanUp();
}

public interface IDrawUpdate
{
    public void DrawUpdate();
}

public interface IDemoGui
{
    public void DrawGui(Ui ui);
}

public interface IDemo
{
    public void Build(Playground pg, World world);
    public string Name { get; }
    public string Description => string.Empty;
    public string Controls => string.Empty;
}
