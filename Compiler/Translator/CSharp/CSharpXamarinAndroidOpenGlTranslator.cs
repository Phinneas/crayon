﻿using System;
using System.Collections.Generic;
using Crayon.ParseTree;

namespace Crayon.Translator.CSharp
{
	/// <summary>
	/// This is actually just a stub class so that the IsOpenGlBased property will return true.
	/// In the future I will be moving towards a hardcoded platform-specific implementation helper class
	/// for each OpenGL platform as putting all the OpenGL renderers in one common translated file proved
	/// to be more trouble than it was worth, after looking more closely at 3 different OpenGL implementations.
	/// </summary>
	class CSharpXamarinAndroidOpenGlTranslator : AbstractOpenGlTranslator
	{
		public override bool IsNewStyle { get { return true; } }

		public override void TranslateGlBeginPolygon(List<string> output, Expression gl)
		{
			throw new NotImplementedException();
		}

		public override void TranslateGlBeginQuads(List<string> output, Expression gl)
		{
			throw new NotImplementedException();
		}

		public override void TranslateGlBindTexture(List<string> output, Expression gl, Expression textureId)
		{
			throw new NotImplementedException();
		}

		public override void TranslateGlColor4(List<string> output, Expression gl, Expression r, Expression g, Expression b, Expression a)
		{
			throw new NotImplementedException();
		}

		public override void TranslateGlDisableTexCoordArray(List<string> output, Expression gl)
		{
			throw new NotImplementedException();
		}

		public override void TranslateGlDisableTexture2D(List<string> output, Expression gl)
		{
			throw new NotImplementedException();
		}

		public override void TranslateGlDisableVertexArray(List<string> output, Expression gl)
		{
			throw new NotImplementedException();
		}

		public override void TranslateGlDrawArrays(List<string> output, Expression gl, Expression vertexCount)
		{
			throw new NotImplementedException();
		}

		public override void TranslateGlDrawEllipseVertices(List<string> output, Expression gl)
		{
			throw new NotImplementedException();
		}

		public override void TranslateGlEnableTexture2D(List<string> output, Expression gl)
		{
			throw new NotImplementedException();
		}

		public override void TranslateGlEnableTextureCoordArray(List<string> output, Expression gl)
		{
			throw new NotImplementedException();
		}

		public override void TranslateGlEnableVertexArray(List<string> output, Expression gl)
		{
			throw new NotImplementedException();
		}

		public override void TranslateGlEnd(List<string> output, Expression gl)
		{
			throw new NotImplementedException();
		}

		public override void TranslateGlFrontFaceCw(List<string> output, Expression gl)
		{
			throw new NotImplementedException();
		}

		public override void TranslateGlGetQuadTextureVbo(List<string> output, Expression gl)
		{
			throw new NotImplementedException();
		}

		public override void TranslateGlGetQuadVbo(List<string> output, Expression gl)
		{
			throw new NotImplementedException();
		}

		public override void TranslateGlLoadIdentity(List<string> output, Expression gl)
		{
			throw new NotImplementedException();
		}

		public override void TranslateGlLoadTexture(List<string> output, Expression gl, Expression platformBitmapResource)
		{
			throw new NotImplementedException();
		}

		public override void TranslateGlMaxTextureSize(List<string> output)
		{
			output.Add("2048");
		}

		public override void TranslateGlPrepareDrawPipeline(List<string> output, Expression gl)
		{
			throw new NotImplementedException();
		}

		public override void TranslateGlScale(List<string> output, Expression gl, Expression xratio, Expression yratio)
		{
			throw new NotImplementedException();
		}

		public override void TranslateGlTexCoord2(List<string> output, Expression gl, Expression x, Expression y)
		{
			throw new NotImplementedException();
		}

		public override void TranslateGlTexCoordPointer(List<string> output, Expression gl, Expression textureBuffer)
		{
			throw new NotImplementedException();
		}

		public override void TranslateGlTranslate(List<string> output, Expression gl, Expression dx, Expression dy)
		{
			throw new NotImplementedException();
		}

		public override void TranslateGlVertex2(List<string> output, Expression gl, Expression x, Expression y)
		{
			throw new NotImplementedException();
		}

		public override void TranslateGlVertexPointer(List<string> output, Expression gl, Expression vertexBuffer)
		{
			throw new NotImplementedException();
		}
	}
}
