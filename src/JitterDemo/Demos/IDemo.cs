namespace JitterDemo;


public interface ICleanDemo
{
    public void CleanUp();
}

public interface IDemo
{
    public void Build();
    public void Draw();
    public string Name { get; }
}