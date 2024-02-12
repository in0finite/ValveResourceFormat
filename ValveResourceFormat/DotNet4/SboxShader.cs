using ValveResourceFormat.Blocks;

namespace ValveResourceFormat.ResourceTypes
{
    public class SboxShader : ResourceData
    {
        public override BlockType Type { get; }

        public SboxShader()
        {
            // Older files use a DATA block which is equivalent to the newer DXBC
            Type = BlockType.DATA;
        }

        public SboxShader(BlockType blockType)
        {
            Type = blockType;
        }
    }
}
