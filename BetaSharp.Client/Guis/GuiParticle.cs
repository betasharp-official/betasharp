using BetaSharp.Client.Rendering;
using BetaSharp.Client.Rendering.Core;
using java.util;

namespace BetaSharp.Client.Guis;

public class GuiParticle : Gui
{
    private readonly List<Particle> particles = new ();
    private readonly BetaSharp _game;

    public GuiParticle(BetaSharp game)
    {
        this._game = game;
    }

    public void updateParticles()
    {
        for (int i = 0; i < particles.Count; ++i)
        {
            Particle p = particles[i];
            p.UpdatePrevious();
            p.Update(this);
            if (p.PendingRemoval)
            {
                particles.RemoveAt(i--);
            }
        }

    }

    public void render(float partialTicks)
    {
        _game.textureManager.BindTexture(_game.textureManager.GetTextureId("/gui/particles.png"));

        for (int i = 0; i < particles.Count; ++i)
        {
            Particle p = particles[i];
            int x = (int)(p.PrevX + (p.X - p.PrevX) * partialTicks - 4);
            int y = (int)(p.PrevY + (p.Y - p.PrevY) * partialTicks - 4);
            float alpha = (float)(p.PrevAlpha + (p.Alpha - p.PrevAlpha) * partialTicks);
            float r = (float)(p.PrevR + (p.R - p.PrevR) * partialTicks);
            float g = (float)(p.PrevG + (p.G - p.PrevG) * partialTicks);
            float b = (float)(p.PrevB + (p.B - p.PrevB) * partialTicks);
            GLManager.GL.Color4(r, g, b, alpha);
            DrawTexturedModalRect(x, y, 40, 0, 8, 8);
        }

    }
}
