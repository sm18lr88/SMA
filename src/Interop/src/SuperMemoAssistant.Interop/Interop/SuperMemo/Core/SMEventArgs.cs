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
// Modified On:  2020/04/07 00:30
// Modified By:  Alexis

#endregion




namespace SuperMemoAssistant.Interop.SuperMemo.Core
{
  using System;
  using System.Diagnostics;
  using Content;
  using Content.Components;
  using Content.Models;
  using Elements.Models;
  using Elements.Types;
  using Registry.Members;

  /// <summary>Base event, contains a SM management instance</summary>
  [Serializable]
  public class SMEventArgs : EventArgs
  {
    #region Constructors

    /// <summary>New instance</summary>
    /// <param name="smMgmt"></param>
    public SMEventArgs(ISuperMemo smMgmt)
    {
      SMMgmt = smMgmt;
    }

    #endregion




    #region Properties & Fields - Public

    /// <summary>The SM service</summary>
    public ISuperMemo SMMgmt { get; set; }

    #endregion
  }

  /// <summary>SuperMemo App Process-related events, contains a definition of its Process</summary>
  [Serializable]
  public class SMProcessEventArgs : SMEventArgs
  {
    #region Constructors

    /// <summary>Instantiate</summary>
    /// <param name="smMgmt"></param>
    /// <param name="process"></param>
    public SMProcessEventArgs(ISuperMemo smMgmt,
                              Process    process)
      : base(smMgmt)
    {
      Process = process;
    }

    #endregion




    #region Properties & Fields - Public

    /// <summary>The SuperMemo process</summary>
    public Process Process { get; }

    #endregion
  }

  /// <summary>Element-related events</summary>
  [Serializable]
  public class SMDisplayedElementChangedEventArgs : SMEventArgs
  {
    #region Constructors

    /// <summary>New instance</summary>
    /// <param name="smMgmt"></param>
    /// <param name="newElement"></param>
    /// <param name="oldElement"></param>
    public SMDisplayedElementChangedEventArgs(ISuperMemo smMgmt,
                                              IElement   newElement,
                                              IElement   oldElement)
      : base(smMgmt)
    {
      NewElement = newElement;
      OldElement = oldElement;
    }

    #endregion




    #region Properties & Fields - Public

    /// <summary>The new element being displayed</summary>
    public IElement NewElement { get; }

    /// <summary>The old element displayed before</summary>
    public IElement OldElement { get; }

    #endregion
  }

  /// <summary>Element-related events</summary>
  [Serializable]
  public class SMElementEventArgs : SMEventArgs
  {
    #region Constructors

    /// <summary>New instance</summary>
    /// <param name="smMgmt"></param>
    /// <param name="element"></param>
    public SMElementEventArgs(ISuperMemo smMgmt,
                              IElement   element)
      : base(smMgmt)
    {
      Element = element;
    }

    #endregion




    #region Properties & Fields - Public

    /// <summary>The element</summary>
    public IElement Element { get; }

    #endregion
  }

  /// <summary>Element change-related events</summary>
  [Serializable]
  public class SMElementChangedEventArgs : SMEventArgs
  {
    #region Constructors

    /// <summary>New instance</summary>
    /// <param name="smMgmt"></param>
    /// <param name="element"></param>
    /// <param name="changedFields"></param>
    public SMElementChangedEventArgs(ISuperMemo        smMgmt,
                                     IElement          element,
                                     ElementFieldFlags changedFields)
      : base(smMgmt)
    {
      Element       = element;
      ChangedFields = changedFields;
    }

    #endregion




    #region Properties & Fields - Public

    /// <summary>The element which changed</summary>
    public IElement Element { get; }

    /// <summary>Defines which fields have changed</summary>
    public ElementFieldFlags ChangedFields { get; }

    #endregion
  }

  /// <summary>Registry member-related events</summary>
  [Serializable]
  public class SMRegistryEventArgs<T> : SMEventArgs
    where T : IRegistryMember
  {
    #region Constructors

    /// <summary>New instance</summary>
    /// <param name="smMgmt"></param>
    /// <param name="member"></param>
    public SMRegistryEventArgs(ISuperMemo smMgmt,
                               T          member)
      : base(smMgmt)
    {
      Member = member;
    }

    #endregion




    #region Properties & Fields - Public

    /// <summary>The registry member</summary>
    public T Member { get; }

    #endregion
  }

  /// <summary>Component-related events</summary>
  [Serializable]
  public class SMComponentChangedEventArgs : SMEventArgs
  {
    #region Constructors

    /// <summary>New instance</summary>
    /// <param name="smMgmt"></param>
    /// <param name="component"></param>
    /// <param name="changedFields"></param>
    public SMComponentChangedEventArgs(ISuperMemo          smMgmt,
                                       IComponent          component,
                                       ComponentFieldFlags changedFields)
      : base(smMgmt)
    {
      Component     = component;
      ChangedFields = changedFields;
    }

    #endregion




    #region Properties & Fields - Public

    /// <summary>The changed component</summary>
    public IComponent Component { get; }

    /// <summary>Defines which fields got changed</summary>
    public ComponentFieldFlags ChangedFields { get; }

    #endregion
  }

  /// <summary>Component group-related events</summary>
  [Serializable]
  public class SMComponentGroupEventArgs : SMEventArgs
  {
    #region Constructors

    /// <summary>New instance</summary>
    /// <param name="smMgmt"></param>
    /// <param name="componentGroup"></param>
    public SMComponentGroupEventArgs(ISuperMemo      smMgmt,
                                     IComponentGroup componentGroup)
      : base(smMgmt)
    {
      ComponentGroup = componentGroup;
    }

    #endregion




    #region Properties & Fields - Public

    /// <summary>The component group</summary>
    public IComponentGroup ComponentGroup { get; }

    #endregion
  }
}
