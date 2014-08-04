
using ComponentAce.Compression.Libs.zlib;
using System;
using System.IO;
using System.IO.Compression;
namespace Fan.Utils
{
    public static class ZipUtil
    {
        
        //public static byte[] Compress(byte[] data)
        //{
        //    using(MemoryStream ms = new MemoryStream())
        //    {
        //        using(GZipStream gzs = new GZipStream(ms,CompressionMode.Compress))
        //        {
        //            gzs.Write(data,0,data.Length);
        //            return ms.ToArray();
        //        }
        //    }
        //}

        //public static byte[] Decompress(byte[] data)
        //{
        //    using (MemoryStream ms = new MemoryStream(data))
        //    {
        //        using (GZipStream gzs = new GZipStream(ms, CompressionMode.Decompress))
        //        {
        //            byte[] result = new byte[gzs.Length];
        //            gzs.Write(result,0,result.Length);
        //            return result;
        //        }
        //    }
        //}


        public const int BUFFER_SIZE = 1024 * 1024;
        private static byte[] _buffer = new byte[BUFFER_SIZE];

        public static byte[] Decompress(byte[] data)
        {
            ZStream zs = new ZStream();
            zs.next_in = data;
            zs.avail_in = data.Length;
            zs.next_out = _buffer;
            zs.avail_out = _buffer.Length;

            if (zlibConst.Z_OK != zs.inflateInit())
                return null;
            if (zlibConst.Z_STREAM_END != zs.inflate(zlibConst.Z_FINISH))
                return null;
            zs.inflateEnd();

            byte[] result = new byte[zs.total_out];
            Array.Copy(_buffer, result, zs.total_out);
            return result;
        }

   
        public static byte[] Compress(byte[] data)
        {
            ZStream zs = new ZStream();
            zs.next_in = data;
            zs.avail_in = data.Length;
            zs.next_out = _buffer;
            zs.avail_out = _buffer.Length;

            if (zlibConst.Z_OK != zs.deflateInit(zlibConst.Z_BEST_COMPRESSION))
                return null;
            if (zlibConst.Z_STREAM_END != zs.deflate(zlibConst.Z_FINISH))
                return null;
            zs.deflateEnd();
            byte[] result = new byte[zs.total_out];
            Array.Copy(_buffer, result, zs.total_out);
            return result;
        }

    }
}
