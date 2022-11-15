﻿using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Scripture;


namespace SIL.Machine.Corpora
{
	// NB:  Prevent ambiguous reference for .Zip extension method.
	using SIL.Extensions;

	public class VerseRefComparer : IComparer<VerseRef>
	{
		private readonly bool _compareSegments;

		public VerseRefComparer(bool compareSegments = true)
		{
			_compareSegments = compareSegments;
		}

		public int Compare(VerseRef x, VerseRef y)
		{
			if (!x.HasMultiple && !y.HasMultiple)
				return x.CompareTo(y, null, compareAllVerses: false, _compareSegments);

			// Correctly implement comparing all verses in a range or sequence. The implementation of
			// VerseRef.CompareTo() that compares all verses does not handle segments correctly.
			if (x.Versification != y.Versification)
				y.ChangeVersification(x.Versification);
			VerseRef[] xArray = x.AllVerses().ToArray();
			VerseRef[] yArray = y.AllVerses().ToArray();
			foreach ((VerseRef sx, VerseRef sy) in xArray.Zip(yArray))
			{
				int compare = sx.CompareTo(sy, null, compareAllVerses: false, _compareSegments);
				if (compare != 0)
					return compare;
			}
			return xArray.Length.CompareTo(yArray.Length);
		}
	}
}
