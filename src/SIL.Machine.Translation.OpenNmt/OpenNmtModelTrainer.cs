using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Python.Runtime;
using SIL.Machine.Corpora;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.OpenNmt
{
	public class OpenNmtModelTrainer : DisposableBase, ITranslationModelTrainer
	{
		private static readonly dynamic opennmt = OpenNmt.Module;

		private readonly string _configFileName;
		private readonly ITokenProcessor _sourcePreprocessor;
		private readonly ITokenProcessor _targetPreprocessor;
		private readonly ParallelTextCorpus _corpus;
		private readonly int _maxCorpusCount;
		private readonly HashSet<int> _valCorpusIndices;

		public OpenNmtModelTrainer(string configFileName, ITokenProcessor sourcePreprocessor,
			ITokenProcessor targetPreprocessor, ParallelTextCorpus corpus, int maxCorpusCount = int.MaxValue)
		{
			_configFileName = configFileName;
			_sourcePreprocessor = sourcePreprocessor;
			_targetPreprocessor = targetPreprocessor;
			_corpus = corpus;
			_maxCorpusCount = maxCorpusCount;
			_valCorpusIndices = CreateValCorpus();
		}

		public SmtBatchTrainStats Stats { get; } = new SmtBatchTrainStats();

		public void Train(IProgress<ProgressStatus> progress = null, Action checkCanceled = null)
		{
			using (Py.GIL())
			{
				dynamic config = opennmt.load_config(new[] { _configFileName });
				dynamic dataConfig = config["data"];

				string trainSrcFileName = dataConfig["train_features_file"];
				string trainTrgFileName = dataConfig["train_labels_file"];
				string valSrcFileName = dataConfig["eval_features_file"];
				string valTrgFileName = dataConfig["eval_labels_file"];
				string srcVocabFileName = dataConfig["source_vocabulary"];
				string trgVocabFileName = dataConfig["target_vocabulary"];
				WriteCorpusFiles(trainSrcFileName, trainTrgFileName, valSrcFileName, valTrgFileName, srcVocabFileName,
					trgVocabFileName);
			}

			var psi = new ProcessStartInfo
			{
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				UseShellExecute = false,
				FileName = "onmt-main",
				Arguments = $"--config {_configFileName} --auto_config --model_type TransformerBase --mixed_precision train"
			};

			using (var process = new Process { StartInfo = psi })
			{
				process.OutputDataReceived += (sender, args) =>
				{
				};
				process.Start();
				process.BeginOutputReadLine();
				process.WaitForExit();
			}
		}

		private HashSet<int> CreateValCorpus()
		{
			int corpusCount = 0;
			var invalidIndices = new HashSet<int>();
			int index = 0;
			foreach (ParallelTextSegment segment in _corpus.Segments)
			{
				if (IsSegmentValid(segment))
					corpusCount++;
				else
					invalidIndices.Add(index);
				index++;
				if (corpusCount == _maxCorpusCount)
					break;
			}
			Stats.TrainedSegmentCount = corpusCount;
			int valCorpusCount = Math.Min((int)(corpusCount * 0.1), 1000);
			var r = new Random(31415);
			return new HashSet<int>(Enumerable.Range(0, corpusCount + invalidIndices.Count)
				.Where(i => !invalidIndices.Contains(i))
				.OrderBy(i => r.Next()).Take(valCorpusCount));
		}

		private void WriteCorpusFiles(string srcTrainFileName, string trgTrainFileName, string srcValFileName,
			string trgValFileName, string srcVocabFileName, string trgVocabFileName)
		{
			dynamic srcVocab = opennmt.data.Vocab(new[] { "<blank>", "<s>", "</s>" });
			dynamic trgVocab = opennmt.data.Vocab(new[] { "<blank>", "<s>", "</s>" });
			int corpusCount = 0;
			int index = 0;
			var utf8Encoding = new UTF8Encoding(false);
			using (var srcTrainWriter = new StreamWriter(srcTrainFileName, false, utf8Encoding))
			using (var trgTrainWriter = new StreamWriter(trgTrainFileName, false, utf8Encoding))
			using (var srcValWriter = new StreamWriter(srcValFileName, false, utf8Encoding))
			using (var trgValWriter = new StreamWriter(trgValFileName, false, utf8Encoding))
			{
				foreach (ParallelTextSegment segment in _corpus.GetSegments())
				{
					if (IsSegmentValid(segment))
					{
						if (_valCorpusIndices.Contains(index))
						{
							srcValWriter.WriteLine(string.Join(" ",
								_sourcePreprocessor.Process(segment.SourceSegment)));
							trgValWriter.WriteLine(string.Join(" ",
								_targetPreprocessor.Process(segment.TargetSegment)));
						}
						else
						{
							srcTrainWriter.WriteLine(string.Join(" ",
								_sourcePreprocessor.Process(segment.SourceSegment)));
							trgTrainWriter.WriteLine(string.Join(" ",
								_targetPreprocessor.Process(segment.TargetSegment)));
						}

						AddSegmentToVocab(srcVocab, _sourcePreprocessor, segment.SourceSegment);
						AddSegmentToVocab(trgVocab, _targetPreprocessor, segment.TargetSegment);

						corpusCount++;
					}
					index++;
					if (corpusCount == _maxCorpusCount)
						break;
				}
			}

			srcVocab = srcVocab.prune();
			trgVocab = trgVocab.prune();

			srcVocab.pad_to_multiple(8);
			trgVocab.pad_to_multiple(8);

			srcVocab.serialize(srcVocabFileName);
			trgVocab.serialize(trgVocabFileName);
		}

		private static void AddSegmentToVocab(dynamic vocab, ITokenProcessor preprocessor,
			IReadOnlyList<string> segment)
		{
			foreach (string token in preprocessor.Process(segment))
				vocab.add(token);
		}

		private static bool IsSegmentValid(ParallelTextSegment segment)
		{
			return !segment.IsEmpty && segment.SourceSegment.Count <= TranslationConstants.MaxSegmentLength
				&& segment.TargetSegment.Count <= TranslationConstants.MaxSegmentLength;
		}

		public void Save()
		{
		}

		public Task SaveAsync()
		{
			return Task.CompletedTask;
		}
	}
}
