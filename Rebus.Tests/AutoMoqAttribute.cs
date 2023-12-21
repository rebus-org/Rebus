using System;
using AutoFixture.AutoMoq;
using AutoFixture.NUnit3;
using AutoFixture;

namespace Rebus.Tests;

[AttributeUsage(AttributeTargets.Method)]
public class AutoMoqAttribute : AutoDataAttribute
{
    public AutoMoqAttribute() : base(CreateFixture) { }

    private static IFixture CreateFixture()
    {
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization { ConfigureMembers = true, GenerateDelegates = true });
        return fixture;
    }
}