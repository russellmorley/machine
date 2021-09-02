﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.Plugin;
using SIL.Machine.Translation;
using SIL.Machine.Translation.Thot;

namespace SIL.Machine
{
	public class AlignmentModelCommandSpec : ICommandSpec
	{
		private CommandArgument _modelArgument;
		private CommandOption _modelTypeOption;
		private CommandOption _pluginOption;

		private IWordAlignmentModelFactory _modelFactory;

		public bool IsSymmetric
		{
			get
			{
				if (_modelFactory != null)
					return _modelFactory.IsSymmetric;

				return false;
			}
		}

		public void AddParameters(CommandBase command)
		{
			_modelArgument = command.Argument("MODEL_PATH", "The word alignment model.").IsRequired();
			_modelTypeOption = command.Option("-mt|--model-type <MODEL_TYPE>",
				$"The word alignment model type.\nTypes: \"{ToolHelpers.Hmm}\" (default), \"{ToolHelpers.Ibm1}\", \"{ToolHelpers.Ibm2}\", \"{ToolHelpers.FastAlign}\".",
				CommandOptionType.SingleValue);
			_pluginOption = command.Option("-mp|--model-plugin <PLUGIN_FILE>", "The model plugin file.",
				CommandOptionType.SingleValue);
		}

		public bool Validate(TextWriter outWriter)
		{
			if (_pluginOption.HasValue() && _pluginOption.Values.Any(p => !File.Exists(p)))
			{
				outWriter.WriteLine("A specified plugin file does not exist.");
				return false;
			}

			var pluginLoader = new PluginManager(_pluginOption.Values);
			var factories = pluginLoader.Create<IWordAlignmentModelFactory>().ToDictionary(f => f.ModelType);

			if (!ValidateAlignmentModelTypeOption(_modelTypeOption.Value(), factories.Keys))
			{
				outWriter.WriteLine("The specified word alignment model type is invalid.");
				return false;
			}

			if (_modelTypeOption.HasValue() && factories.TryGetValue(_modelTypeOption.Value(),
				out IWordAlignmentModelFactory factory))
			{
				_modelFactory = factory;
			}

			return true;
		}

		public IWordAlignmentModel CreateAlignmentModel(
			WordAlignmentDirection direction = WordAlignmentDirection.Symmetric,
			SymmetrizationHeuristic symHeuristic = SymmetrizationHeuristic.Och)
		{
			if (_modelFactory != null)
				return _modelFactory.CreateModel(_modelArgument.Value, direction, symHeuristic);

			ThotWordAlignmentModelType modelType = ToolHelpers.GetThotWordAlignmentModelType(_modelTypeOption.Value());

			string modelPath = _modelArgument.Value;
			if (ToolHelpers.IsDirectoryPath(modelPath))
				modelPath = Path.Combine(modelPath, "src_trg");

			if (direction == WordAlignmentDirection.Direct)
			{
				var directModel = ThotWordAlignmentModel.Create(modelType);
				directModel.Load(modelPath + "_invswm");
				return directModel;
			}
			else if (direction == WordAlignmentDirection.Inverse)
			{
				var inverseModel = ThotWordAlignmentModel.Create(modelType);
				inverseModel.Load(modelPath + "_swm");
				return inverseModel;
			}
			else
			{
				var directModel = ThotWordAlignmentModel.Create(modelType);
				directModel.Load(modelPath + "_invswm");

				var inverseModel = ThotWordAlignmentModel.Create(modelType);
				inverseModel.Load(modelPath + "_swm");

				return new SymmetrizedWordAlignmentModel(directModel, inverseModel) { Heuristic = symHeuristic };
			}
		}

		public bool IsSegmentInvalid(ParallelTextSegment segment)
		{
			return segment.IsEmpty;
		}

		public ITrainer CreateAlignmentModelTrainer(ParallelTextCorpus corpus, int maxSize, ITokenProcessor processor,
			Dictionary<string, string> parameters, bool direct = true)
		{
			if (_modelFactory != null)
			{
				return _modelFactory.CreateTrainer(_modelArgument.Value, processor, processor, corpus, maxSize,
					parameters, direct);
			}

			ThotWordAlignmentModelType modelType = ToolHelpers.GetThotWordAlignmentModelType(_modelTypeOption.Value());

			string modelPath = _modelArgument.Value;
			if (ToolHelpers.IsDirectoryPath(modelPath))
				modelPath = Path.Combine(modelPath, "src_trg");
			string modelDir = Path.GetDirectoryName(modelPath);
			if (!Directory.Exists(modelDir))
				Directory.CreateDirectory(modelDir);

			int iters = -1;
			if (parameters.TryGetValue("iters", out string itersStr))
				iters = int.Parse(itersStr);

			bool? varBayes = null;
			if (parameters.TryGetValue("var-bayes", out string varBayesStr))
				varBayes = bool.Parse(varBayesStr);

			string modelStr;
			ParallelTextCorpus trainCorpus;
			if (direct)
			{
				modelStr = "invswm";
				trainCorpus = corpus;
			}
			else
			{
				modelStr = "swm";
				trainCorpus = corpus.Invert();
			}

			var trainer = new ThotWordAlignmentModelTrainer(modelType, $"{modelPath}_{modelStr}", processor, processor,
				trainCorpus, maxSize);
			if (iters != -1)
				trainer.TrainingIterationCount = iters;
			if (varBayes != null)
				trainer.VariationalBayes = (bool)varBayes;
			return trainer;
		}

		private static bool ValidateAlignmentModelTypeOption(string value, IEnumerable<string> pluginTypes)
		{
			var validTypes = new HashSet<string>
			{
				ToolHelpers.Hmm,
				ToolHelpers.Ibm1,
				ToolHelpers.Ibm2,
				ToolHelpers.FastAlign
			};
			validTypes.UnionWith(pluginTypes);
			return string.IsNullOrEmpty(value) || validTypes.Contains(value);
		}
	}
}
