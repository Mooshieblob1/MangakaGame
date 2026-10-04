using System;
using Godot;

namespace MangakaGame;

public partial class OfficeView
{
    // The studio corner (2026-10-03): with Helper-Chan's desk on the studio island, her old alcove holds the tools and
    // storage of a working manga studio. Decoration only, outside every furniture cell; it never blocks a route.
    private void BuildStudioCorner()
    {
        var corner = new Node3D { Name = "StudioCorner" }; _room.AddChild(corner);
        var wood = new Color(.55f, .38f, .24f); var pale = new Color(.86f, .80f, .68f); var steel = new Color(.62f, .66f, .66f);
        var paper = new Color(.96f, .95f, .90f); var ink = new Color(.10f, .10f, .12f);
        var random = new Random(7); // the same spines in every career
        Color Spine() => Color.FromHsv((float)random.NextDouble(), .45f + (float)random.NextDouble() * .3f, .55f + (float)random.NextDouble() * .3f);

        // Reference bookcase on the back wall: manga volumes and art books, a few lying flat.
        OfficeArt.Box(corner, new(-2.25f, .9f, .2f), new(1.1f, 1.8f, .34f), wood);
        for (var shelf = 0; shelf < 4; shelf++)
        {
            var y = .12f + shelf * .43f;
            OfficeArt.Box(corner, new(-2.25f, y - .03f, .23f), new(1.02f, .03f, .3f), wood.Lightened(.15f));
            for (var x = -2.72f; x < -1.85f;)
            {
                var width = .035f + (float)random.NextDouble() * .03f; var height = .22f + (float)random.NextDouble() * .1f;
                OfficeArt.Box(corner, new(x + width / 2, y + height / 2, .25f), new(width, height, .2f), Spine());
                x += width + .006f;
            }
            if (shelf % 2 == 1) OfficeArt.Box(corner, new(-1.86f, y + .05f, .25f), new(.16f, .1f, .22f), Spine());
        }

        // Plan chest for finished manuscript pages: wide shallow drawers, with the next issue's paper on top.
        OfficeArt.Box(corner, new(-1.05f, .38f, .32f), new(.9f, .76f, .58f), steel);
        for (var drawer = 0; drawer < 5; drawer++)
        {
            OfficeArt.Box(corner, new(-1.05f, .1f + drawer * .145f, .615f), new(.84f, .005f, .01f), steel.Darkened(.3f));
            OfficeArt.Box(corner, new(-1.05f, .17f + drawer * .145f, .62f), new(.16f, .02f, .015f), steel.Darkened(.45f));
        }
        for (var sheet = 0; sheet < 6; sheet++) OfficeArt.Box(corner, new(-1.18f + sheet * .004f, .77f + sheet * .006f, .3f), new(.36f, .005f, .26f), paper);
        // Ink bottles, a pen cup and a ruler beside the paper.
        OfficeArt.Box(corner, new(-.8f, .81f, .22f), new(.06f, .08f, .06f), ink);
        OfficeArt.Box(corner, new(-.72f, .8f, .3f), new(.05f, .06f, .05f), new(.18f, .25f, .55f));
        OfficeArt.Box(corner, new(-.74f, .83f, .44f), new(.08f, .12f, .08f), new(.75f, .3f, .25f));
        for (var pen = 0; pen < 4; pen++) OfficeArt.Box(corner, new(-.76f + pen * .013f, .92f, .44f), new(.008f, .1f, .008f), pen % 2 == 0 ? ink : steel);
        OfficeArt.Box(corner, new(-.98f, .776f, .5f), new(.42f, .004f, .04f), new(.85f, .9f, .95f, .8f));

        // Cork board above the chest with Name storyboards and a deadline calendar pinned up.
        OfficeArt.Box(corner, new(-1.05f, 1.55f, .07f), new(.95f, .6f, .02f), new(.72f, .55f, .36f));
        for (var page = 0; page < 6; page++)
        {
            var at = new Vector3(-1.38f + page % 3 * .32f, 1.68f - page / 3 * .27f, .085f);
            OfficeArt.Box(corner, at, new(.2f, .24f, .006f), paper);
            for (var panel = 0; panel < 3; panel++) OfficeArt.Box(corner, at + new Vector3(0, .07f - panel * .07f, .004f), new(.16f, .004f, .002f), ink);
            OfficeArt.Box(corner, at + new Vector3(0, .11f, .006f), new(.02f, .02f, .004f), new(.85f, .2f, .2f));
        }

        // Light table on the side wall for tracing and inking, its frosted top glowing softly.
        OfficeArt.Box(corner, new(-2.62f, .36f, 1.95f), new(.62f, .72f, .9f), pale);
        var glow = new StandardMaterial3D { AlbedoColor = new(.98f, .98f, .92f), EmissionEnabled = true, Emission = new(.95f, .95f, .85f), EmissionEnergyMultiplier = .6f };
        corner.AddChild(new MeshInstance3D { Position = new(-2.62f, .735f, 1.95f), Mesh = new BoxMesh { Size = new(.56f, .02f, .8f) }, MaterialOverride = glow });
        OfficeArt.Box(corner, new(-2.6f, .75f, 1.9f), new(.25f, .004f, .34f), paper);
        OfficeArt.Box(corner, new(-2.48f, .86f, 2.32f), new(.03f, .22f, .03f), steel.Darkened(.2f)); // lamp arm
        OfficeArt.Box(corner, new(-2.56f, .97f, 2.24f), new(.16f, .06f, .12f), new(.9f, .74f, .3f));

        // Screentone rack: labelled sheets standing in slots.
        OfficeArt.Box(corner, new(-2.72f, .45f, 2.85f), new(.42f, .9f, .6f), wood);
        for (var slot = 0; slot < 9; slot++)
            OfficeArt.Box(corner, new(-2.58f, .5f + slot % 3 * .02f, 2.6f + slot * .06f), new(.3f, .62f + slot % 3 * .05f, .006f), slot % 3 == 0 ? new(.82f, .82f, .84f) : slot % 3 == 1 ? new(.7f, .7f, .74f) : paper);

        // Doujin stock waiting for the next event: printed boxes, one open with books inside.
        OfficeArt.Box(corner, new(-2.65f, .17f, 3.62f), new(.5f, .34f, .4f), new(.71f, .56f, .38f));
        OfficeArt.Box(corner, new(-2.62f, .46f, 3.6f), new(.42f, .24f, .34f), new(.74f, .59f, .4f));
        OfficeArt.Box(corner, new(-2.05f, .14f, 3.65f), new(.44f, .28f, .36f), new(.71f, .56f, .38f));
        for (var book = 0; book < 4; book++) OfficeArt.Box(corner, new(-2.15f + book * .07f, .3f, 3.65f), new(.05f, .06f, .26f), Spine());

        // A stack of weekly magazines by the bookcase, and a plant for the long nights.
        for (var issue = 0; issue < 7; issue++)
            OfficeArt.Box(corner, new(-1.6f + (issue % 2) * .01f, .02f + issue * .025f, .55f), new(.26f, .022f, .36f), Spine());
        OfficeArt.Box(corner, new(-.55f, .16f, 3.7f), new(.26f, .32f, .26f), new(.78f, .48f, .32f));
        foreach (var leaf in new[] { new Vector3(-.58f, .5f, 3.68f), new Vector3(-.5f, .58f, 3.74f), new Vector3(-.56f, .66f, 3.7f) })
            OfficeArt.Box(corner, leaf, new(.2f, .18f, .2f), new(.33f, .55f, .3f));
    }
}
