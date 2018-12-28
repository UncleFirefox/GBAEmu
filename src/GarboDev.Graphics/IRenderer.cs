using GarboDev.CrossCutting;

namespace GarboDev.Graphics
{
    public interface IRenderer
    {
        Memory Memory { set; }
        void Initialize(object data);
        void Reset();
        void RenderLine(int line);
        object ShowFrame();
    }
}
