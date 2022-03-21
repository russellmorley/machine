﻿using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	[TestFixture]
	public class UsfmFileTextTests
	{
		[Test]
		public void GetRows_NonEmptyText()
		{
			var corpus = new UsfmFileTextCorpus("usfm.sty", Encoding.UTF8, CorporaTestHelpers.UsfmTestProjectPath);

			IText text = corpus["MAT"];
			TextCorpusRow[] rows = text.GetRows().ToArray();
			Assert.That(rows.Length, Is.EqualTo(14));

			Assert.That(rows[0].Ref, Is.EqualTo(new VerseRef("MAT 1:1", corpus.Versification)));
			Assert.That(rows[0].Text, Is.EqualTo("Chapter one, verse one."));

			Assert.That(rows[1].Ref, Is.EqualTo(new VerseRef("MAT 1:2", corpus.Versification)));
			Assert.That(rows[1].Text, Is.EqualTo("Chapter one, verse two."));

			Assert.That(rows[4].Ref, Is.EqualTo(new VerseRef("MAT 1:5", corpus.Versification)));
			Assert.That(rows[4].Text, Is.EqualTo("Chapter one, verse five."));

			Assert.That(rows[5].Ref, Is.EqualTo(new VerseRef("MAT 2:1", corpus.Versification)));
			Assert.That(rows[5].Text, Is.EqualTo("Chapter two, verse one."));

			Assert.That(rows[6].Ref, Is.EqualTo(new VerseRef("MAT 2:2", corpus.Versification)));
			Assert.That(rows[6].Text, Is.EqualTo("Chapter two, verse two. Chapter two, verse three."));
			Assert.That(rows[6].IsInRange, Is.True);
			Assert.That(rows[6].IsRangeStart, Is.True);

			Assert.That(rows[7].Ref, Is.EqualTo(new VerseRef("MAT 2:3", corpus.Versification)));
			Assert.That(rows[7].Text, Is.Empty);
			Assert.That(rows[7].IsInRange, Is.True);
			Assert.That(rows[7].IsRangeStart, Is.False);

			Assert.That(rows[8].Ref, Is.EqualTo(new VerseRef("MAT 2:4a", corpus.Versification)));
			Assert.That(rows[8].Text, Is.Empty);
			Assert.That(rows[8].IsInRange, Is.True);
			Assert.That(rows[7].IsRangeStart, Is.False);

			Assert.That(rows[9].Ref, Is.EqualTo(new VerseRef("MAT 2:4b", corpus.Versification)));
			Assert.That(rows[9].Text, Is.EqualTo("Chapter two, verse four."));

			Assert.That(rows[10].Ref, Is.EqualTo(new VerseRef("MAT 2:5", corpus.Versification)));
			Assert.That(rows[10].Text, Is.EqualTo("Chapter two, verse five."));

			Assert.That(rows[11].Ref, Is.EqualTo(new VerseRef("MAT 2:6", corpus.Versification)));
			Assert.That(rows[11].Text, Is.EqualTo("Chapter two, verse six."));
		}

		[Test]
		public void GetRows_SentenceStart()
		{
			var corpus = new UsfmFileTextCorpus("usfm.sty", Encoding.UTF8, CorporaTestHelpers.UsfmTestProjectPath);

			IText text = corpus["MAT"];
			TextCorpusRow[] segments = text.GetRows().ToArray();
			Assert.That(segments.Length, Is.EqualTo(14));

			Assert.That(segments[3].Ref, Is.EqualTo(new VerseRef("MAT 1:4", corpus.Versification)));
			Assert.That(segments[3].Text, Is.EqualTo("Chapter one, verse four,"));
			Assert.That(segments[3].IsSentenceStart, Is.True);

			Assert.That(segments[4].Ref, Is.EqualTo(new VerseRef("MAT 1:5", corpus.Versification)));
			Assert.That(segments[4].Text, Is.EqualTo("Chapter one, verse five."));
			Assert.That(segments[4].IsSentenceStart, Is.False);
		}

		[Test]
		public void GetRows_EmptyText()
		{
			var corpus = new UsfmFileTextCorpus("usfm.sty", Encoding.UTF8, CorporaTestHelpers.UsfmTestProjectPath);

			IText text = corpus["MRK"];
			TextCorpusRow[] segments = text.GetRows().ToArray();
			Assert.That(segments, Is.Empty);
		}

		[Test]
		public void GetRows_IncludeMarkers()
		{
			var corpus = new UsfmFileTextCorpus("usfm.sty", Encoding.UTF8, CorporaTestHelpers.UsfmTestProjectPath,
				includeMarkers: true);

			IText text = corpus["MAT"];
			TextCorpusRow[] segments = text.GetRows().ToArray();
			Assert.That(segments.Length, Is.EqualTo(14));

			Assert.That(segments[0].Ref, Is.EqualTo(new VerseRef("MAT 1:1", corpus.Versification)));
			Assert.That(segments[0].Text, Is.EqualTo(
				"Chapter \\pn one\\+pro WON\\+pro*\\pn*, verse one.\\f + \\fr 1:1: \\ft This is a footnote.\\f*"));

			Assert.That(segments[1].Ref, Is.EqualTo(new VerseRef("MAT 1:2", corpus.Versification)));
			Assert.That(segments[1].Text, Is.EqualTo("Chapter one, \\li2 verse\\f + \\fr 1:2: \\ft This is a footnote.\\f* two."));

			Assert.That(segments[4].Ref, Is.EqualTo(new VerseRef("MAT 1:5", corpus.Versification)));
			Assert.That(segments[4].Text,
				Is.EqualTo("Chapter one, \\li2 verse \\fig Figure 1|src=\"image1.png\" size=\"col\" ref=\"1:5\"\\fig* five."));

			Assert.That(segments[5].Ref, Is.EqualTo(new VerseRef("MAT 2:1", corpus.Versification)));
			Assert.That(segments[5].Text, Is.EqualTo(
				"Chapter \\add two\\add*, verse \\f + \\fr 2:1: \\ft This is a footnote.\\f*one."));

			Assert.That(segments[6].Ref, Is.EqualTo(new VerseRef("MAT 2:2", corpus.Versification)));
			Assert.That(segments[6].Text,
				Is.EqualTo("Chapter two, verse \\fm ∆\\fm*two. Chapter two, verse three."));
			Assert.That(segments[6].IsInRange, Is.True);
			Assert.That(segments[6].IsRangeStart, Is.True);

			Assert.That(segments[7].Ref, Is.EqualTo(new VerseRef("MAT 2:3", corpus.Versification)));
			Assert.That(segments[7].Text, Is.Empty);
			Assert.That(segments[7].IsInRange, Is.True);
			Assert.That(segments[7].IsRangeStart, Is.False);

			Assert.That(segments[8].Ref, Is.EqualTo(new VerseRef("MAT 2:4a", corpus.Versification)));
			Assert.That(segments[8].Text, Is.Empty);
			Assert.That(segments[8].IsInRange, Is.True);
			Assert.That(segments[8].IsRangeStart, Is.False);

			Assert.That(segments[9].Ref, Is.EqualTo(new VerseRef("MAT 2:4b", corpus.Versification)));
			Assert.That(segments[9].Text, Is.EqualTo("Chapter two, verse four."));

			Assert.That(segments[10].Ref, Is.EqualTo(new VerseRef("MAT 2:5", corpus.Versification)));
			Assert.That(segments[10].Text, Is.EqualTo("Chapter two, verse five \\rq (MAT 3:1)\\rq*."));

			Assert.That(segments[11].Ref, Is.EqualTo(new VerseRef("MAT 2:6", corpus.Versification)));
			Assert.That(segments[11].Text, Is.EqualTo("Chapter two, verse \\w six|strong=\"12345\" \\w*."));
		}
	}
}
