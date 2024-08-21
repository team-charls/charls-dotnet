// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppression either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.
// False warnings are suppressed here to keep the code clean.
// Warnings that are suppressed by a design choice are suppressed in the source code.
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "CA1027:Mark enums with FlagsAttribute", Justification = "False warnings (triggered by missing values), enum values cannot be combined", Scope = "type", Target = "~T:CharLS.Managed.SpiffColorSpace")]
[assembly: SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "Values are external defined by ISO", Scope = "type", Target = "~T:CharLS.Managed.SpiffEntryTag")]
[assembly: SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "False positive", Scope = "member", Target = "~F:CharLS.Managed.ScanCodec.RegularModeContextArray._")]
[assembly: SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "False positive", Scope = "member", Target = "~F:CharLS.Managed.ScanCodec.RunModeContextArray._")]
[assembly: SuppressMessage("Style", "IDE0032:Use auto property", Justification = "Private field is more clear", Scope = "member", Target = "~F:CharLS.Managed.JpegStreamWriter._position")]
[assembly: SuppressMessage("Style", "IDE0251:Make member 'readonly'", Justification = "False positive", Scope = "member", Target = "~M:CharLS.Managed.JpegStreamReader.RaiseApplicationDataEvent(CharLS.Managed.JpegMarkerCode)")]
[assembly: SuppressMessage("Major Code Smell", "S1172:Unused method parameters should be removed", Justification = "False positive", Scope = "member", Target = "~M:CharLS.Managed.CopyToLineBuffer.CopySamples8Bit(System.ReadOnlySpan{System.Byte},System.Span{System.Byte},System.Int32,System.Int32)")]
[assembly: SuppressMessage("Major Code Smell", "S1172:Unused method parameters should be removed", Justification = "False positive", Scope = "member", Target = "~M:CharLS.Managed.CopyToLineBuffer.CopySamples16Bit(System.ReadOnlySpan{System.Byte},System.Span{System.Byte},System.Int32,System.Int32)")]
[assembly: SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "False positive", Scope = "member", Target = "~F:CharLS.Managed.ScanCodec.RunModeContextArray._")]
[assembly: SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "False positive", Scope = "member", Target = "~F:CharLS.Managed.ScanCodec.RegularModeContextArray._")]
