﻿// -- FILE ------------------------------------------------------------------
// name       : RtfInterpreterListenerDocumentBuilder.cs
// project    : RTF Framelet
// created    : Leon Poyyayil - 2008.05.21
// language   : c#
// environment: .NET 2.0
// copyright  : (c) 2004-2013 by Jani Giannoudis, Switzerland
// --------------------------------------------------------------------------
using System.Text;
using RtfPipe.Model;
using System.Collections.Generic;

namespace RtfPipe.Interpreter
{

	// ------------------------------------------------------------------------
	public sealed class RtfInterpreterListenerDocumentBuilder : RtfInterpreterListenerBase
	{

		// ----------------------------------------------------------------------
		public bool CombineTextWithSameFormat
		{
			get { return combineTextWithSameFormat; }
			set { combineTextWithSameFormat = value; }
		} // CombineTextWithSameFormat

		// ----------------------------------------------------------------------
		public IRtfDocument Document
		{
			get { return document; }
		} // Document

		// ----------------------------------------------------------------------
		protected override void DoBeginDocument( IRtfInterpreterContext context )
		{
			document = null;
			visualDocumentContent = new List<IRtfVisual>();
		} // DoBeginDocument

		// ----------------------------------------------------------------------
		protected override void DoInsertText( IRtfInterpreterContext context, string text )
		{
			if ( combineTextWithSameFormat )
			{
				IRtfTextFormat newFormat = context.GetSafeCurrentTextFormat();
				if ( !newFormat.Equals( pendingTextFormat ) )
				{
					FlushPendingText();
				}
				pendingTextFormat = newFormat;
				pendingText.Append( text );
			}
			else
			{
				AppendAlignedVisual( new RtfVisualText( text, context.GetSafeCurrentTextFormat() ) );
			}
		} // DoInsertText

		// ----------------------------------------------------------------------
		protected override void DoInsertSpecialChar( IRtfInterpreterContext context, RtfVisualSpecialCharKind kind )
		{
			FlushPendingText();
			visualDocumentContent.Add( new RtfVisualSpecialChar( kind, context.GetSafeCurrentTextFormat()) );
		} // DoInsertSpecialChar

		// ----------------------------------------------------------------------
		protected override void DoInsertBreak( IRtfInterpreterContext context, RtfVisualBreakKind kind )
		{
			FlushPendingText();
			visualDocumentContent.Add( new RtfVisualBreak( kind ) );
			switch ( kind )
			{
				case RtfVisualBreakKind.Paragraph:
				case RtfVisualBreakKind.Section:
					EndParagraph( context );
					break;
			}
		} // DoInsertBreak

		// ----------------------------------------------------------------------
		protected override void DoInsertImage( IRtfInterpreterContext context,
			RtfVisualImageFormat format,
			int width, int height, int desiredWidth, int desiredHeight,
			int scaleWidthPercent, int scaleHeightPercent,
			string imageDataHex
		)
		{
			FlushPendingText();
			AppendAlignedVisual( new RtfVisualImage( format,
				context.GetSafeCurrentTextFormat().Alignment,
				width, height, desiredWidth, desiredHeight,
				scaleWidthPercent, scaleHeightPercent, imageDataHex ) );
		} // DoInsertImage

		// ----------------------------------------------------------------------
		protected override void DoEndDocument( IRtfInterpreterContext context )
		{
			FlushPendingText();
			EndParagraph( context );
			document = new RtfDocument( context, visualDocumentContent );
			visualDocumentContent = null;
			visualDocumentContent = null;
		} // DoEndDocument

		// ----------------------------------------------------------------------
		private void EndParagraph( IRtfInterpreterContext context )
		{
			RtfTextAlignment finalParagraphAlignment = context.GetSafeCurrentTextFormat().Alignment;
			foreach ( IRtfVisual alignedVisual in pendingParagraphContent )
			{
				switch ( alignedVisual.Kind )
				{
					case RtfVisualKind.Image:
						RtfVisualImage image = (RtfVisualImage)alignedVisual;
						// ReSharper disable RedundantCheckBeforeAssignment
						if ( image.Alignment != finalParagraphAlignment )
						// ReSharper restore RedundantCheckBeforeAssignment
						{
							image.Alignment = finalParagraphAlignment;
						}
						break;
					case RtfVisualKind.Text:
						RtfVisualText text = (RtfVisualText)alignedVisual;
						if ( text.Format.Alignment != finalParagraphAlignment )
						{
							IRtfTextFormat correctedFormat = ( (RtfTextFormat)text.Format ).DeriveWithAlignment( finalParagraphAlignment );
							IRtfTextFormat correctedUniqueFormat = context.GetUniqueTextFormatInstance( correctedFormat );
							text.Format = correctedUniqueFormat;
						}
						break;
				}
			}
			pendingParagraphContent.Clear();
		} // EndParagraph

		// ----------------------------------------------------------------------
		private void FlushPendingText()
		{
			if ( pendingTextFormat != null )
			{
				AppendAlignedVisual( new RtfVisualText( pendingText.ToString(), pendingTextFormat ) );
				pendingTextFormat = null;
				pendingText.Remove( 0, pendingText.Length );
			}
		} // FlushPendingText

		// ----------------------------------------------------------------------
		private void AppendAlignedVisual( RtfVisual visual )
		{
			visualDocumentContent.Add( visual );
			pendingParagraphContent.Add( visual );
		} // AppendAlignedVisual

		// ----------------------------------------------------------------------
		// members
		private bool combineTextWithSameFormat = true;

		private RtfDocument document;
		private IList<IRtfVisual> visualDocumentContent;
		private readonly IList<IRtfVisual> pendingParagraphContent = new List<IRtfVisual>();

		private IRtfTextFormat pendingTextFormat;
		private readonly StringBuilder pendingText = new StringBuilder();

	} // class RtfInterpreterListenerDocumentBuilder

} // namespace RtfPipe.Interpreter
// -- EOF -------------------------------------------------------------------
