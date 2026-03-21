namespace JitterTests.Behavior;

public class WorldCallbackTests
{
    [TestCase]
    public void PreAndPostSubStep_FireOncePerSubstep_WithSubstepDt()
    {
        var world = new World
        {
            SubstepCount = 4
        };

        int preCount = 0, postCount = 0;
        Real preDt = 0, postDt = 0;

        world.PreSubStep += dt =>
        {
            preCount++;
            preDt = dt;
        };

        world.PostSubStep += dt =>
        {
            postCount++;
            postDt = dt;
        };

        const Real dt = 1f / 60f;
        world.Step(dt, false);

        Assert.That(preCount, Is.EqualTo(4));
        Assert.That(postCount, Is.EqualTo(4));
        Assert.That(preDt, Is.EqualTo(dt / 4).Within(1e-6f));
        Assert.That(postDt, Is.EqualTo(dt / 4).Within(1e-6f));
        world.Dispose();
    }

    [TestCase]
    public void SubStepCallbacks_AreOrderedWithinEachSubstep()
    {
        var world = new World
        {
            SubstepCount = 3
        };

        List<string> events = [];

        world.PreSubStep += _ => events.Add("pre");
        world.PostSubStep += _ => events.Add("post");

        world.Step(1f / 60f, false);

        Assert.That(events, Has.Count.EqualTo(6));

        for (int i = 0; i < events.Count; i += 2)
        {
            Assert.That(events[i], Is.EqualTo("pre"));
            Assert.That(events[i + 1], Is.EqualTo("post"));
        }

        world.Dispose();
    }
}
