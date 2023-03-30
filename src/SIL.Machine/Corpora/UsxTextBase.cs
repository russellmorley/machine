﻿using System.Collections.Generic;
using System.IO;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public abstract class UsxTextBase : ScriptureText
	{
		private readonly UsxVerseParser _parser;

		protected UsxTextBase(string id, ScrVers versification)
			: base(id, versification)
		{
			_parser = new UsxVerseParser();
		}

		protected override IEnumerable<TextRow> GetVersesInDocOrder()
		{
			using (IStreamContainer streamContainer = CreateStreamContainer())
			using (Stream stream = streamContainer.OpenStream())
			{
				foreach (UsxVerse verse in _parser.Parse(stream))
				{
					foreach (
						TextRow segment in CreateRows(
							CreateVerseRef(verse.Chapter, verse.Verse),
							verse.Text,
							verse.IsSentenceStart
						)
					)
					{
						yield return segment;
					}
				}
			}
		}

		protected abstract IStreamContainer CreateStreamContainer();
	}
}
