#region License & Metadata

// The MIT License (MIT)
// 
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// 
// 
// Created On:   2020/03/29 00:21
// Modified On:  2020/04/06 18:22
// Modified By:  Alexis

#endregion




namespace Hjg.Pngcs.Chunks
{
  using System;
  using System.Globalization;

  /// <summary>tIME chunk: http://www.w3.org/TR/PNG/#11tIME</summary>
  public class PngChunkTIME : PngChunkSingle
  {
    #region Constants & Statics

    public const String ID = ChunkHelper.tIME;

    #endregion




    #region Properties & Fields - Non-Public

    private int year, mon, day, hour, min, sec;

    #endregion




    #region Constructors

    public PngChunkTIME(ImageInfo info)
      : base(ID, info) { }

    #endregion




    #region Methods Impl

    public override ChunkOrderingConstraint GetOrderingConstraint()
    {
      return ChunkOrderingConstraint.NONE;
    }

    public override ChunkRaw CreateRawChunk()
    {
      ChunkRaw c = createEmptyChunk(7, true);
      PngHelperInternal.WriteInt2tobytes(year, c.Data, 0);
      c.Data[2] = (byte)mon;
      c.Data[3] = (byte)day;
      c.Data[4] = (byte)hour;
      c.Data[5] = (byte)min;
      c.Data[6] = (byte)sec;
      return c;
    }

    public override void ParseFromRaw(ChunkRaw chunk)
    {
      if (chunk.Length != 7)
        throw new PngjException("bad chunk " + chunk);

      year = PngHelperInternal.ReadInt2fromBytes(chunk.Data, 0);
      mon  = PngHelperInternal.ReadInt1fromByte(chunk.Data, 2);
      day  = PngHelperInternal.ReadInt1fromByte(chunk.Data, 3);
      hour = PngHelperInternal.ReadInt1fromByte(chunk.Data, 4);
      min  = PngHelperInternal.ReadInt1fromByte(chunk.Data, 5);
      sec  = PngHelperInternal.ReadInt1fromByte(chunk.Data, 6);
    }

    public override void CloneDataFromRead(PngChunk other)
    {
      PngChunkTIME x = (PngChunkTIME)other;
      year = x.year;
      mon  = x.mon;
      day  = x.day;
      hour = x.hour;
      min  = x.min;
      sec  = x.sec;
    }

    #endregion




    #region Methods

    public void SetNow(int secsAgo)
    {
      DateTime d1 = DateTime.Now;
      year = d1.Year;
      mon  = d1.Month;
      day  = d1.Day;
      hour = d1.Hour;
      min  = d1.Minute;
      sec  = d1.Second;
    }

    internal void SetYMDHMS(int yearx, int monx, int dayx, int hourx, int minx, int secx)
    {
      year = yearx;
      mon  = monx;
      day  = dayx;
      hour = hourx;
      min  = minx;
      sec  = secx;
    }

    public int[] GetYMDHMS()
    {
      return new int[] { year, mon, day, hour, min, sec };
    }

    /**
     * format YYYY/MM/DD HH:mm:SS
     */
    public string GetAsString()
    {
      return string.Format(CultureInfo.InvariantCulture,
                           "%04d/%02d/%02d %02d:%02d:%02d",
                           year, mon, day, hour, min, sec);
    }

    #endregion
  }
}
