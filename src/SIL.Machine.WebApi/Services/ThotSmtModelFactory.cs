﻿namespace SIL.Machine.WebApi.Services;

public class ThotSmtModelFactory : ISmtModelFactory
{
	private readonly IOptions<ThotSmtModelOptions> _options;
	private readonly IOptions<EngineOptions> _engineOptions;

	public ThotSmtModelFactory(IOptions<ThotSmtModelOptions> options, IOptions<EngineOptions> engineOptions)
	{
		_options = options;
		_engineOptions = engineOptions;
	}

	public IInteractiveTranslationModel Create(string engineId)
	{
		string smtConfigFileName = Path.Combine(_engineOptions.Value.EnginesDir, engineId, "smt.cfg");
		var model = new ThotSmtModel(ThotWordAlignmentModelType.Hmm, smtConfigFileName);
		return model;
	}

	public ITrainer CreateTrainer(string engineId, IEnumerable<ParallelTextRow> corpus)
	{
		string smtConfigFileName = Path.Combine(_engineOptions.Value.EnginesDir, engineId, "smt.cfg");
		return new ThotSmtModelTrainer(ThotWordAlignmentModelType.Hmm, corpus, smtConfigFileName);
	}

	public void InitNew(string engineId)
	{
		string engineDir = Path.Combine(_engineOptions.Value.EnginesDir, engineId);
		if (!Directory.Exists(engineDir))
			Directory.CreateDirectory(engineDir);
		ZipFile.ExtractToDirectory(_options.Value.NewModelFile, engineDir);
	}

	public void Cleanup(string engineId)
	{
		string engineDir = Path.Combine(_engineOptions.Value.EnginesDir, engineId);
		if (!Directory.Exists(engineDir))
			return;
		string lmDir = Path.Combine(engineDir, "lm");
		if (Directory.Exists(lmDir))
			Directory.Delete(lmDir, true);
		string tmDir = Path.Combine(engineDir, "tm");
		if (Directory.Exists(tmDir))
			Directory.Delete(tmDir, true);
		string smtConfigFileName = Path.Combine(engineDir, "smt.cfg");
		if (File.Exists(smtConfigFileName))
			File.Delete(smtConfigFileName);
		if (!Directory.EnumerateFileSystemEntries(engineDir).Any())
			Directory.Delete(engineDir);
	}
}
