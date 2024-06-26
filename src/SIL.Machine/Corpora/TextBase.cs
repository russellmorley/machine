﻿using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public abstract class TextBase : IText
	{
		protected TextBase(string id, string sortKey)
		{
			Id = id;
			SortKey = sortKey;
		}

		public string Id { get; }

		public string SortKey { get; }

		public abstract IEnumerable<TextRow> GetRows();

		protected TextRow CreateRow(string text, object segRef,
			bool isSentenceStart = true, bool isInRange = false, bool isRangeStart = false)
		{
			text = text.Trim();
			return new TextRow(segRef)
			{
				Segment = new[] { text },
				IsSentenceStart = isSentenceStart,
				IsInRange = isInRange,
				IsRangeStart = isRangeStart,
				IsEmpty = text.Length == 0
			};
		}

		protected TextRow CreateEmptyRow(object segRef, bool isInRange = false)
		{
			return new TextRow(segRef) { IsInRange = isInRange };
		}
	}
}
