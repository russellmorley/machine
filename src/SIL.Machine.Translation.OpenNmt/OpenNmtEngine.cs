using System;
using System.Collections.Generic;
using System.Text;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.OpenNmt
{
	public class OpenNmtEngine : DisposableBase, ITranslationEngine
	{
		public TranslationResult Translate(IReadOnlyList<string> segment)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<TranslationResult> Translate(int n, IReadOnlyList<string> segment)
		{
			throw new NotImplementedException();
		}
	}
}
