using System.Buffers.Binary;
namespace MangakaSim;

public static class ImageDimensions
{
    public static (int Width,int Height) Read(byte[] data)
    {
        if(data.Length>=24&&data.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}))
        {
            var w=BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(16,4));var h=BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(20,4));
            if(w is 0 or >4096||h is 0 or >4096)throw new InvalidDataException("Image dimensions must be between 1 and 4096 pixels.");
            return ((int)w,(int)h);
        }
        if(data.Length>=4&&data[0]==255&&data[1]==216)
        {
            var p=2;
            while(p+4<=data.Length)
            {
                if(data[p++]!=255)throw new InvalidDataException("Malformed JPEG.");
                while(p<data.Length&&data[p]==255)p++;
                if(p>=data.Length)break;var marker=data[p++];
                if(marker is 0xd8 or 0x01||marker is >=0xd0 and <=0xd7)continue;
                if(marker is 0xda or 0xd9)break;
                if(p+2>data.Length)break;var size=BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(p,2));
                if(size<2||p+size>data.Length)break;
                if(marker is 0xc0 or 0xc1 or 0xc2 or 0xc3 or 0xc5 or 0xc6 or 0xc7 or 0xc9 or 0xca or 0xcb or 0xcd or 0xce or 0xcf)
                {if(size<7)break;var h=BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(p+3,2));var w=BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(p+5,2));if(w is 0 or >4096||h is 0 or >4096)break;return(w,h);}
                p+=size;
            }
        }
        throw new InvalidDataException("Choose a valid PNG or JPEG, at most 4096 pixels per side.");
    }
}
