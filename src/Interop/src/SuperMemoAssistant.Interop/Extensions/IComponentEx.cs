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
// Created On:   2019/03/02 18:29
// Modified On:  2020/01/13 13:10
// Modified By:  Alexis

#endregion




using SuperMemoAssistant.Interop.SuperMemo.Content.Components;

// ReSharper disable UnusedMember.Global

namespace SuperMemoAssistant.Extensions
{
  public static class IComponentEx
  {
    #region Methods

    public static IComponentHtml AsWeb(this IComponent component)
    {
      return component as IComponentHtml;
    }

    public static IComponentImage AsImage(this IComponent component)
    {
      return component as IComponentImage;
    }

    public static IComponentRtf AsRtf(this IComponent component)
    {
      return component as IComponentRtf;
    }

    public static IComponentShapeEllipse AsEllipse(this IComponent component)
    {
      return component as IComponentShapeEllipse;
    }

    public static IComponentShapeRectangle AsRectangle(this IComponent component)
    {
      return component as IComponentShapeRectangle;
    }

    public static IComponentShapeRoundedRectangle AsRoundedRectangle(this IComponent component)
    {
      return component as IComponentShapeRoundedRectangle;
    }

    public static IComponentSound AsSound(this IComponent component)
    {
      return component as IComponentSound;
    }

    public static IComponentSpelling AsSpelling(this IComponent component)
    {
      return component as IComponentSpelling;
    }

    public static IComponentText AsText(this IComponent component)
    {
      return component as IComponentText;
    }

    public static IComponentVideo AsVideo(this IComponent component)
    {
      return component as IComponentVideo;
    }

    #endregion
  }
}
