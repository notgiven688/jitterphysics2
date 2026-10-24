using Vellum;

namespace JitterDemo;

public interface IDemo
{
    public void Build(Playground pg);
    public void CleanUp() { }
    public void DrawUpdate() { }
    public void DrawGui(Ui ui) { }
    public string Name { get; }
    public string Description => string.Empty;
    public string Controls => string.Empty;
}
