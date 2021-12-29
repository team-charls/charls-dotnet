// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Text;

namespace CharLS.JpegLS.Test;

internal class PortableAnymapFile
{
    internal int ComponentCount { get; set; }
    internal int Width { get; set; }
    internal int Height { get; set; }
    internal int BitsPerSample { get; set; }

    internal byte[] ImageData { get; }

    internal PortableAnymapFile(string filename)
    {
        using var fs = new FileStream(filename, FileMode.Open, FileAccess.Read);

        List<int> headerInfo = ReadHeader(fs);
        if (headerInfo.Count != 4)
            throw new InvalidDataException("Incorrect PNM header");

        ComponentCount = headerInfo[0] == 6 ? 3 : 1;
        Width = headerInfo[1];
        Height = headerInfo[2];
        BitsPerSample = Log2(headerInfo[3] + 1);

        int bytesPerSample = (BitsPerSample + 7) / 8;

        byte[] imageData = new byte[Width * Height * bytesPerSample * ComponentCount];
        int bytesRead = fs.Read(imageData, 0, imageData.Length);
        if (bytesRead != imageData.Length)
            throw new InvalidDataException("Missing image data");

        ImageData = imageData;
        // convert_to_little_endian_if_needed();
    }

    private static List<int> ReadHeader(Stream pnmFile)
    {
        List<int> result = new();

        byte first = (byte)pnmFile.ReadByte();

        // All portable anymap format (PNM) start with the character P.
        if (first != 'P')
            throw new InvalidDataException("Missing P");

        using UnbufferedStreamReader sr = new(pnmFile);

        while (result.Count < 4)
        {
            string line = sr.ReadLine();
            string[] tokens = line.Split();
            foreach (var token in tokens)
            {
                if (int.TryParse(token, out int value))
                {
                    result.Add(value);
                }
            }
        }

        return result;
    }

    private static int Log2(int n)
    {
        int x = 0;
        while (n > (1 << x))
        {
            ++x;
        }
        return x;
    }

    private sealed class UnbufferedStreamReader : TextReader
    {
        private readonly Stream _s;
        
        public UnbufferedStreamReader(Stream stream)
        {
            _s = stream;
        }

        // This method assumes lines end with a line feed.
        // You may need to modify this method if your stream
        // follows the Windows convention of \r\n or some other 
        // convention that isn't just \n
        public override string ReadLine()
        {
            List<byte> bytes = new();
            int current;
            while ((current = Read()) != -1 && current != (int)'\n')
            {
                byte b = (byte)current;
                bytes.Add(b);
            }
            return Encoding.ASCII.GetString(bytes.ToArray());
        }

        // Read works differently than the `Read()` method of a 
        // TextReader. It reads the next BYTE rather than the next character
        public override int Read()
        {
            return _s.ReadByte();
        }

        public override void Close()
        {
        }

        protected override void Dispose(bool disposing)
        {
        }

        public override int Peek()
        {
            throw new NotImplementedException();
        }

        public override int Read(char[] buffer, int index, int count)
        {
            throw new NotImplementedException();
        }

        public override int ReadBlock(char[] buffer, int index, int count)
        {
            throw new NotImplementedException();
        }

        public override string ReadToEnd()
        {
            throw new NotImplementedException();
        }
    }
}


//void convert_to_little_endian_if_needed() noexcept
//{
//    // Anymap files with multi byte pixels are stored in big endian format in the file.
//    if (bits_per_sample_ > 8)
//    {
//        for (size_t i{ }; i < input_buffer_.size() - 1; i += 2)
//            {
//            std::swap(input_buffer_[i], input_buffer_[i + 1]);
//        }
//    }
//}
