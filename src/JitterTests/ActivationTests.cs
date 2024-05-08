namespace JitterTests;

public class ActivationTests
{
    private World world = null!;

    [SetUp]
    public void Setup()
    {
        world = new World(79, 93, 67);
    }

    [TestCase]
    public void Can_Create_Active_Body()
    {
        var body = world.CreateRigidBody(true);
        Assert.That(body.IsActive);
    }

    [TestCase]
    public void Can_Create_Sleeping_Body()
    {
        var body = world.CreateRigidBody(false);
        Assert.That(!body.IsActive);

        //7 days, 7 nights
        world.Step(TimeSpan.FromDays(7).Seconds);
        Assert.That(!body.IsActive);

        //and snow-white slept for 300 years
        world.Step(TimeSpan.FromDays(300 * 365).Seconds);
        Assert.That(!body.IsActive);
    }

    [TestCase]
    public void Body_Is_Active_By_Default()
    {
        var body = world.CreateRigidBody();
        Assert.That(body.IsActive);
    }

    [TestCase]
    public void Bodies_Fall_Asleep_On_Time()
    {
        var body = world.CreateRigidBody(true);
        body.deactivationTimeThreshold = 123f;

        world.Step(123f);
        world.Step(float.Epsilon);
        Assert.That(!body.IsActive);
    }

    [TestCase]
    public void Bodies_Do_Not_Fall_Asleep_Early()
    {
        var body = world.CreateRigidBody(true);
        body.deactivationTimeThreshold = 4f;

        body.sleepTime = 0;
        world.Step(1);
        Assert.That(body.IsActive);

        body.sleepTime = 0;
        world.Step(4);
        Assert.That(body.IsActive);
    }

    [TestCase]
    public void Sleeping_Body_Stays_Asleep()
    {
        var body = world.CreateRigidBody(false);

        //are you sleeping?
        world.Step(0);
        Assert.That(!body.IsActive);

        //what if you blink?
        world.Step(0.016f);
        Assert.That(!body.IsActive);

        //7 days, 7 nights
        world.Step(TimeSpan.FromDays(7).Seconds);
        Assert.That(!body.IsActive);

        //and snow-white slept for 300 years...
        world.Step(TimeSpan.FromDays(300 * 365).Seconds);
        Assert.That(!body.IsActive);

        //until the end of time
        world.Step(float.MaxValue);
        Assert.That(!body.IsActive);
    }

    [TestCase]
    public void NullBody_is_Asleep()
    {
        Assert.That(!world.NullBody.IsActive);
        world.Step(1);
        Assert.That(!world.NullBody.IsActive);
    }

    [TestCase]
    public void Static_Bodies_Do_Not_Wake_Up()
    {
        world.NullBody.SetActivationState(true);
        world.Step(1);
        Assert.That(!world.NullBody.IsActive);

        var body = world.CreateRigidBody(false);
        body.IsStatic = true;
        body.SetActivationState(true);
        world.Step(1);
        Assert.That(!body.IsActive);
    }

    [TestCase]
    public void Static_Bodies_Retain_Activation_State()
    {
        var body = world.CreateRigidBody(true);
        body.deactivationTimeThreshold = 9000;
        body.IsStatic = true;
        world.Step(1);
        Assert.That(body.IsActive);

        body.IsStatic = false;
        world.Step(1);
        Assert.That(body.IsActive);
        
        body.IsStatic = true;
        world.Step(float.MaxValue);
        Assert.That(body.IsActive);
    }

    [TestCase]
    public void Sleeping_Body_Can_Be_Activated_After_Epsilon_Time()
    {
        var body = world.CreateRigidBody(false);

        body.SetActivationState(true);
        world.Step(float.Epsilon);
        Assert.That(body.IsActive);
    }

    [TestCase]
    public void Sleeping_Body_Can_Be_Activated_After_Max_Time()
    {
        var body = world.CreateRigidBody(false);

        body.SetActivationState(true);
        world.Step(float.MaxValue);
        Assert.That(body.IsActive);
    }

    /* This doesn't work yet because World.Step() is idempotent at zero timestep
    [TestCase]
    public void Sleeping_Body_Can_Be_Activated_Immediately()
    {
        var body = world.CreateRigidBody(false);
        body.SetActivationState(true);
        world.Step(0);
        Assert.That(body.IsActive);
    }
    */
}