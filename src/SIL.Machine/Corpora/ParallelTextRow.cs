﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class ParallelTextRow : IRow
	{
		public ParallelTextRow(IReadOnlyList<object> sourceRefs, IReadOnlyList<object> targetRefs)
		{
			if (sourceRefs.Count == 0 && targetRefs.Count == 0)
				throw new ArgumentNullException("Either a source or target ref must be provided.");

			SourceRefs = sourceRefs;
			TargetRefs = targetRefs;
		}

		public object Ref => SourceRefs.Count > 0 ? SourceRefs[0] : TargetRefs[0];

		public IReadOnlyList<object> SourceRefs { get; }
		public IReadOnlyList<object> TargetRefs { get; }

		public IReadOnlyList<string> SourceSegment { get; set; } = Array.Empty<string>();

		public IReadOnlyList<string> TargetSegment { get; set; } = Array.Empty<string>();

		public IReadOnlyCollection<AlignedWordPair> AlignedWordPairs { get; set; }

		public bool IsSourceSentenceStart { get; set; } = true;
		public bool IsSourceInRange { get; set; }
		public bool IsSourceRangeStart { get; set; }
		public bool IsTargetSentenceStart { get; set; } = true;
		public bool IsTargetInRange { get; set; }
		public bool IsTargetRangeStart { get; set; }

		public bool IsEmpty { get; set; } = true;

		public string SourceText => string.Join(" ", SourceSegment);
		public string TargetText => string.Join(" ", TargetSegment);

		public ParallelTextRow Invert()
		{
			return new ParallelTextRow(TargetRefs, SourceRefs)
			{
				SourceSegment = TargetSegment,
				TargetSegment = SourceSegment,
				AlignedWordPairs = AlignedWordPairs == null ? null
					: new HashSet<AlignedWordPair>(AlignedWordPairs.Select(wp => wp.Invert())),
				IsSourceSentenceStart = IsTargetSentenceStart,
				IsSourceInRange = IsTargetInRange,
				IsSourceRangeStart = IsTargetRangeStart,
				IsTargetSentenceStart = IsSourceSentenceStart,
				IsTargetInRange = IsSourceInRange,
				IsTargetRangeStart = IsSourceRangeStart,
				IsEmpty = IsEmpty
			};
		}
	}
}
