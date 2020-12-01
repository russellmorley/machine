using System;
using System.Collections.Generic;
using System.Text;
using SIL.Machine.Corpora;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.OpenNmt
{
	public class OpenNmtModel : DisposableBase, ITranslationModel
	{
		public ITranslationEngine CreateEngine()
		{
			throw new NotImplementedException();
		}

		public ITranslationModelTrainer CreateTrainer(ITokenProcessor sourcePreprocessor,
			ITokenProcessor targetPreprocessor, ParallelTextCorpus corpus, int maxCorpusCount = int.MaxValue)
		{
			throw new NotImplementedException();
		}
	}
}
