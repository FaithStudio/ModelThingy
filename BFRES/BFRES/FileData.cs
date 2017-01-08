﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BFRES
{
    public class FileData
    {
        public byte[] b;
        private int p = 0;
        public Endianness Endian = Endianness.Big;
        public string fname = "";
        public string Magic = "";

        public FileData(String f)
        {
            b = File.ReadAllBytes(f);
            fname = Path.GetFileName(f);
            readMagic();
        }

        public FileData(byte[] b)
        {
            this.b = b;
            readMagic();
        }

        public FileData(byte[] b, string f)
        {
            this.b = b;
            fname = f;
            readMagic();
        }

        public void readMagic()
        {
            Magic = readString(0, 4);
        }

        public int eof()
        {
            return b.Length;
        }

        public byte[] read(int length)
        {
            if (length + p > b.Length)
                throw new IndexOutOfRangeException();

            var data = new byte[length];
            for (int i = 0; i < length; i++, p++)
            {
                data[i] = b[p];
            }
            return data;
        }
        public int readInt()
        {
            if (Endian == Endianness.Little)
            {
                return (b[p++] & 0xFF) | ((b[p++] & 0xFF) << 8) | ((b[p++] & 0xFF) << 16) | ((b[p++] & 0xFF) << 24);
            }
            else
                return ((b[p++] & 0xFF) << 24) | ((b[p++] & 0xFF) << 16) | ((b[p++] & 0xFF) << 8) | (b[p++] & 0xFF);
        }
        public int readThree()
        {
            if (Endian == Endianness.Little)
            {
                return (b[p++] & 0xFF) | ((b[p++] & 0xFF) << 8) | ((b[p++] & 0xFF) << 16);
            }
            else
                return ((b[p++] & 0xFF) << 16) | ((b[p++] & 0xFF) << 8) | (b[p++] & 0xFF);
        }

        public int readShort()
        {
            if (Endian == Endianness.Little)
            {
                return (b[p++] & 0xFF) | ((b[p++] & 0xFF) << 8);
            }
            else
                return ((b[p++] & 0xFF) << 8) | (b[p++] & 0xFF);
        }

        public int readByte()
        {
            return (b[p++] & 0xFF);
        }

        public float readFloat()
        {
            byte[] by = new byte[4];
            if (Endian == Endianness.Little)
                by = new byte[4] { b[p], b[p + 1], b[p + 2], b[p + 3] };
            else
                by = new byte[4] { b[p + 3], b[p + 2], b[p + 1], b[p] };
            p += 4;
            return BitConverter.ToSingle(by, 0);
        }

        public float readHalfFloat()
        {
            return toFloat((short)readShort());
        }

        public static float toFloat(int hbits)
        {
            int mant = hbits & 0x03ff;            // 10 bits mantissa
            int exp = hbits & 0x7c00;            // 5 bits exponent
            if (exp == 0x7c00)                   // NaN/Inf
                exp = 0x3fc00;                    // -> NaN/Inf
            else if (exp != 0)                   // normalized value
            {
                exp += 0x1c000;                   // exp - 15 + 127
                if (mant == 0 && exp > 0x1c400)  // smooth transition
                    return BitConverter.ToSingle(BitConverter.GetBytes((hbits & 0x8000) << 16
                        | exp << 13 | 0x3ff), 0);
            }
            else if (mant != 0)                  // && exp==0 -> subnormal
            {
                exp = 0x1c400;                    // make it normal
                do
                {
                    mant <<= 1;                   // mantissa * 2
                    exp -= 0x400;                 // decrease exp by 1
                } while ((mant & 0x400) == 0); // while not normal
                mant &= 0x3ff;                    // discard subnormal bit
            }                                     // else +/-0 -> +/-0
            return BitConverter.ToSingle(BitConverter.GetBytes(          // combine all parts
                (hbits & 0x8000) << 16          // sign  << ( 31 - 15 )
                | (exp | mant) << 13), 0);         // value << ( 23 - 10 )
        }

        /*public static int fromFloat(float fval, bool littleEndian)
        {
            int fbits = FileOutput.SingleToInt32Bits(fval, littleEndian);
            int sign = fbits >> 16 & 0x8000;          // sign only
            int val = (fbits & 0x7fffffff) + 0x1000; // rounded value

            if (val >= 0x47800000)               // might be or become NaN/Inf
            {                                     // avoid Inf due to rounding
                if ((fbits & 0x7fffffff) >= 0x47800000)
                {                                 // is or must become NaN/Inf
                    if (val < 0x7f800000)        // was value but too large
                        return sign | 0x7c00;     // make it +/-Inf
                    return sign | 0x7c00 |        // remains +/-Inf or NaN
                        (fbits & 0x007fffff) >> 13; // keep NaN (and Inf) bits
                }
                return sign | 0x7bff;             // unrounded not quite Inf
            }
            if (val >= 0x38800000)               // remains normalized value
                return sign | val - 0x38000000 >> 13; // exp - 127 + 15
            if (val < 0x33000000)                // too small for subnormal
                return sign;                      // becomes +/-0
            val = (fbits & 0x7fffffff) >> 23;  // tmp exp for subnormal calc
            return sign | ((fbits & 0x7fffff | 0x800000) // add subnormal bit
                + (0x800000 >> val - 102)     // round depending on cut off
                >> 126 - val);   // div by 2^(1-(exp-127+15)) and >> 13 | exp=0
        }*/

        public static int sign12Bit(int i)
        {
            //		System.out.println(Integer.toBinaryString(i));
            if (((i >> 11) & 0x1) == 1)
            {
                //			System.out.println(i);
                i = ~i;
                i = i & 0xFFF;
                //			System.out.println(Integer.toBinaryString(i));
                //			System.out.println(i);
                i += 1;
                i *= -1;
            }

            return i;
        }

        public static int sign10Bit(int i)
        {
            if (((i >> 9) & 0x1) == 1)
            {
                i = ~i;
                i = i & 0x3FF;
                i += 1;
                i *= -1;
            }

            return i;
        }


        public void skip(int i)
        {
            p += i;
        }
        public void seek(int i)
        {
            p = i;
        }

        public int pos()
        {
            return p;
        }

        public int size()
        {
            return b.Length;
        }

        public string readString()
        {
            string s = "";
            while (b[p] != 0x00)
            {
                s += (char)b[p];
                p++;
            }
            return s;
        }

        public string readString(int size)
        {
            string s = "";
            for(int i = 0; i < size; i++)
            {
                s += (char)b[p];
                p++;
            }
            return s;
        }

        public byte[] getSection(int offset, int size)
        {
            if (size == -1)
                size = b.Length - offset;
            byte[] by = new byte[size];

            Array.Copy(b, offset, by, 0, size);

            return by;
        }

        public string readString(int p, int size)
        {
            if (size == -1)
            {
                String str = "";
                while (p < b.Length)
                {
                    if ((b[p] & 0xFF) != 0x00)
                        str += (char)(b[p] & 0xFF);
                    else
                        break;
                    p++;
                }
                return str;
            }
            
            string str2 = "";
            for (int i = p; i < p + size; i++)
            {
                if (i >= b.Length)
                    return str2;
                if ((b[i] & 0xFF) != 0x00)
                    str2 += (char)(b[i] & 0xFF);
            }
            return str2;
        }

        public void align(int i)
        {
            while (p % i != 0)
                p++;
        }

        public int readOffset()
        {
            return p + readInt();
        }
    }

    public class Decompress
    {
        public static FileData YAZ0(FileData i)
        {
            return new FileData(YAZ0(i.b)) { fname = i.fname};
        }

        private static byte[] YAZ0(byte[] data)
        {
            FileData f = new FileData(data);

            f.Endian = Endianness.Big;
            f.seek(4);
            int uncompressedSize = f.readInt();
            f.seek(0x10);

            byte[] src = f.read(data.Length - 0x10);
            byte[] dst = new byte[uncompressedSize];

            int srcPlace = 0, dstPlace = 0; //current read/write positions

            uint validBitCount = 0; //number of valid bits left in "code" byte
            byte currCodeByte = 0;
            while (dstPlace < uncompressedSize)
            {
                //read new "code" byte if the current one is used up
                if (validBitCount == 0)
                {
                    currCodeByte = src[srcPlace];
                    ++srcPlace;
                    validBitCount = 8;
                }

                if ((currCodeByte & 0x80) != 0)
                {
                    //straight copy
                    dst[dstPlace] = src[srcPlace];
                    dstPlace++;
                    srcPlace++;
                }
                else
                {
                    //RLE part
                    byte byte1 = src[srcPlace];
                    byte byte2 = src[srcPlace + 1];
                    srcPlace += 2;

                    uint dist = (uint)(((byte1 & 0xF) << 8) | byte2);
                    uint copySource = (uint)(dstPlace - (dist + 1));

                    uint numBytes = (uint)(byte1 >> 4);
                    if (numBytes == 0)
                    {
                        numBytes = (uint)(src[srcPlace] + 0x12);
                        srcPlace++;
                    }
                    else
                        numBytes += 2;

                    //copy run
                    for (int i = 0; i < numBytes; ++i)
                    {
                        dst[dstPlace] = dst[copySource];
                        copySource++;
                        dstPlace++;
                    }
                }

                //use next bit from "code" byte
                currCodeByte <<= 1;
                validBitCount -= 1;
            }

            return dst;
        }
    }

    public class Endianness
    {
        public static Endianness Little = new Endianness(), Big = new Endianness() { value = 1};
        public int value = 0;
    }
}
