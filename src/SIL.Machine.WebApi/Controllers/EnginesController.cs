﻿namespace SIL.Machine.WebApi.Controllers;

/// <summary>
/// Machine translation engines
/// </summary>
[Area("Translation")]
[Route("[area]/[controller]", Name = RouteNames.Engines)]
[Produces("application/json")]
public class EnginesController : Controller
{
	private readonly IAuthorizationService _authService;
	private readonly IRepository<Engine> _engines;
	private readonly IRepository<Build> _builds;
	private readonly IRepository<DataFile> _dataFiles;
	private readonly IEngineService _engineService;
	private readonly IDataFileService _dataFileService;
	private readonly IOptions<EngineOptions> _engineOptions;

	public EnginesController(IAuthorizationService authService, IRepository<Engine> engines, IRepository<Build> builds,
		IRepository<DataFile> dataFiles, IEngineService engineService, IDataFileService dataFileService,
		IOptions<EngineOptions> engineOptions)
	{
		_authService = authService;
		_engines = engines;
		_builds = builds;
		_dataFiles = dataFiles;
		_engineService = engineService;
		_dataFileService = dataFileService;
		_engineOptions = engineOptions;
	}

	/// <summary>
	/// Gets all engines.
	/// </summary>
	/// <response code="200">The engines.</response>
	[Authorize(Scopes.ReadEngines)]
	[HttpGet]
	public async Task<IEnumerable<EngineDto>> GetAllAsync()
	{
		var engines = new List<EngineDto>();
		foreach (Engine engine in await _engines.GetAllAsync())
		{
			if (await AuthorizeIsOwnerAsync(engine))
				engines.Add(CreateDto(engine));
		}
		return engines;
	}

	/// <summary>
	/// Gets the specified engine.
	/// </summary>
	/// <param name="id">The engine id.</param>
	/// <response code="200">The engine.</response>
	[Authorize(Scopes.ReadEngines)]
	[HttpGet("{id}")]
	public async Task<ActionResult<EngineDto>> GetAsync(string id)
	{
		Engine? engine = await _engines.GetAsync(id);
		if (engine == null)
			return NotFound();
		if (!await AuthorizeIsOwnerAsync(engine))
			return Forbid();

		return Ok(CreateDto(engine));
	}

	/// <summary>
	/// Creates a new engine.
	/// </summary>
	/// <param name="engine">The new engine properties.</param>
	/// <response code="201">The engine was created successfully.</response>
	[Authorize(Scopes.CreateEngines)]
	[HttpPost]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public async Task<ActionResult<EngineDto>> CreateAsync([FromBody] NewEngineDto engine)
	{
		var newEngine = new Engine
		{
			SourceLanguageTag = engine.SourceLanguageTag,
			TargetLanguageTag = engine.TargetLanguageTag,
			Type = engine.Type,
			Owner = User.Identity!.Name!
		};

		if (!await _engineService.CreateAsync(newEngine))
			return Conflict();
		return Ok(CreateDto(newEngine));
	}

	/// <summary>
	/// Deletes the specified engine.
	/// </summary>
	/// <param name="id">The engine id.</param>
	/// <response code="200">The engine was successfully deleted.</response>
	[Authorize(Scopes.DeleteEngines)]
	[HttpDelete("{id}")]
	public async Task<ActionResult> DeleteAsync(string id)
	{
		Engine? engine = await _engines.GetAsync(id);
		if (engine == null)
			return NotFound();
		if (!await AuthorizeIsOwnerAsync(engine))
			return Forbid();

		if (!await _engineService.DeleteAsync(id))
			return NotFound();
		return Ok();
	}

	/// <summary>
	/// Translates a tokenized segment of text.
	/// </summary>
	/// <param name="id">The engine id.</param>
	/// <param name="segment">The tokenized source segment.</param>
	/// <response code="200">The translation result.</response>
	[Authorize(Scopes.ReadEngines)]
	[HttpPost("{id}/translate")]
	public async Task<ActionResult<TranslationResultDto>> TranslateAsync(string id, [FromBody] string[] segment)
	{
		Engine? engine = await _engines.GetAsync(id);
		if (engine == null)
			return NotFound();
		if (!await AuthorizeIsOwnerAsync(engine))
			return Forbid();

		TranslationResult? result = await _engineService.TranslateAsync(engine.Id, segment);
		if (result == null)
			return NotFound();
		return Ok(CreateDto(result));
	}

	/// <summary>
	/// Translates a tokenized segment of text into the top N results.
	/// </summary>
	/// <param name="id">The engine id.</param>
	/// <param name="n">The number of translations.</param>
	/// <param name="segment">The tokenized source segment.</param>
	/// <response code="200">The translation results.</response>
	[Authorize(Scopes.ReadEngines)]
	[HttpPost("{id}/translate/{n}")]
	public async Task<ActionResult<IEnumerable<TranslationResultDto>>> TranslateAsync(string id, int n,
		[FromBody] string[] segment)
	{
		Engine? engine = await _engines.GetAsync(id);
		if (engine == null)
			return NotFound();
		if (!await AuthorizeIsOwnerAsync(engine))
			return Forbid();

		IEnumerable<TranslationResult>? results = await _engineService.TranslateAsync(engine.Id, n, segment);
		if (results == null)
			return NotFound();
		return Ok(results.Select(CreateDto));
	}

	/// <summary>
	/// Gets the word graph that represents all possible translations of a tokenized segment of text.
	/// </summary>
	/// <param name="id">The engine id.</param>
	/// <param name="segment">The tokenized source segment.</param>
	/// <response code="200">The word graph.</response>
	[Authorize(Scopes.ReadEngines)]
	[HttpPost("{id}/get-word-graph")]
	public async Task<ActionResult<WordGraphDto>> InteractiveTranslateAsync(string id, [FromBody] string[] segment)
	{
		Engine? engine = await _engines.GetAsync(id);
		if (engine == null)
			return NotFound();
		if (!await AuthorizeIsOwnerAsync(engine))
			return Forbid();

		WordGraph? result = await _engineService.GetWordGraphAsync(engine.Id, segment);
		if (result == null)
			return NotFound();
		return Ok(CreateDto(result));
	}

	/// <summary>
	/// Incrementally trains the specified engine with a tokenized segment pair.
	/// </summary>
	/// <param name="id">The engine id.</param>
	/// <param name="segmentPair">The tokenized segment pair.</param>
	/// <response code="200">The engine was trained successfully.</response>
	[Authorize(Scopes.UpdateEngines)]
	[HttpPost("{id}/train-segment")]
	[ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
	public async Task<ActionResult> TrainSegmentAsync(string id, [FromBody] SegmentPairDto segmentPair)
	{
		Engine? engine = await _engines.GetAsync(id);
		if (engine == null)
			return NotFound();
		if (!await AuthorizeIsOwnerAsync(engine))
			return Forbid();

		if (!await _engineService.TrainSegmentAsync(engine.Id, segmentPair.SourceSegment,
			segmentPair.TargetSegment, segmentPair.SentenceStart))
		{
			return NotFound();
		}
		return Ok();
	}

	/// <summary>
	/// Gets all build jobs for the specified engine.
	/// </summary>
	/// <param name="id">The engine id.</param>
	/// <response code="200">The build jobs.</response>
	[Authorize(Scopes.ReadEngines)]
	[HttpGet("{id}/builds")]
	public async Task<ActionResult<IEnumerable<BuildDto>>> GetAllBuildsAsync(string id)
	{
		Engine? engine = await _engines.GetAsync(id);
		if (engine == null)
			return NotFound();
		if (!await AuthorizeIsOwnerAsync(engine))
			return Forbid();

		return Ok((await _builds.GetAllAsync()).Select(CreateDto));
	}

	/// <summary>
	/// Gets the specified build job.
	/// </summary>
	/// <param name="id">The engine id.</param>
	/// <param name="buildId">The build job id.</param>
	/// <param name="minRevision">The minimum revision.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <response code="200">The build job.</response>
	[Authorize(Scopes.ReadEngines)]
	[HttpGet("{id}/builds/{buildId}")]
	public async Task<ActionResult<BuildDto>> GetBuildAsync(string id, string buildId, [FromQuery] long? minRevision,
		CancellationToken cancellationToken)
	{
		Engine? engine = await _engines.GetAsync(id, cancellationToken);
		if (engine == null)
			return NotFound();
		if (!await AuthorizeIsOwnerAsync(engine))
			return Forbid();

		if (minRevision != null)
		{
			EntityChange<Build> change = await TaskEx.Timeout(
				ct => _builds.GetNewerRevisionAsync(buildId, minRevision.Value, ct),
				_engineOptions.Value.BuildLongPollTimeout, cancellationToken);
			return change.Type switch
			{
				EntityChangeType.None => StatusCode(StatusCodes.Status408RequestTimeout),
				EntityChangeType.Delete => NotFound(),
				_ => Ok(CreateDto(change.Entity!)),
			};
		}
		else
		{
			Build? build = await _builds.GetAsync(buildId, cancellationToken);
			if (build == null)
				return NotFound();

			return Ok(CreateDto(build));
		}
	}

	/// <summary>
	/// Starts a build job for the specified engine.
	/// </summary>
	/// <param name="id">The engine id.</param>
	/// <response code="201">The build job was started successfully.</response>
	[Authorize(Scopes.UpdateEngines)]
	[HttpPost("{id}/builds")]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public async Task<ActionResult<BuildDto>> CreateBuildAsync(string id)
	{
		Engine? engine = await _engines.GetAsync(id);
		if (engine == null)
			return NotFound();
		if (!await AuthorizeIsOwnerAsync(engine))
			return Forbid();

		Build? build = await _engineService.StartBuildAsync(id);
		if (build == null)
			return UnprocessableEntity();
		BuildDto dto = CreateDto(build);
		return Created(dto.Href, dto);
	}

	/// <summary>
	/// Gets the currently running build job.
	/// </summary>
	/// <param name="id">The engine id.</param>
	/// <param name="minRevision">The minimum revision.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <response code="200">The build job.</response>
	[Authorize(Scopes.ReadEngines)]
	[HttpGet("{id}/current-build")]
	public async Task<ActionResult<BuildDto>> GetCurrentBuildAsync(string id, [FromQuery] long? minRevision,
		CancellationToken cancellationToken)
	{
		Engine? engine = await _engines.GetAsync(id, cancellationToken);
		if (engine == null)
			return NotFound();
		if (!await AuthorizeIsOwnerAsync(engine))
			return Forbid();

		if (minRevision != null)
		{
			EntityChange<Build> change = await TaskEx.Timeout(
				ct => _builds.GetNewerRevisionByEngineIdAsync(id, minRevision.Value, ct),
				_engineOptions.Value.BuildLongPollTimeout, cancellationToken);
			return change.Type switch
			{
				EntityChangeType.None => StatusCode(StatusCodes.Status408RequestTimeout),
				EntityChangeType.Delete => NoContent(),
				_ => Ok(CreateDto(change.Entity!)),
			};
		}
		else
		{
			Build? build = await _builds.GetByEngineIdAsync(id, cancellationToken);
			if (build == null)
				return NoContent();

			return Ok(CreateDto(build));
		}
	}

	/// <summary>
	/// Cancels the current build job.
	/// </summary>
	/// <param name="id">The engine id.</param>
	/// <response code="200">The build job was cancelled successfully.</response>
	[Authorize(Scopes.UpdateEngines)]
	[HttpPost("{id}/current-build/cancel")]
	[ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
	public async Task<ActionResult> CancelBuildAsync(string id)
	{
		Engine? engine = await _engines.GetAsync(id);
		if (engine == null)
			return NotFound();
		if (!await AuthorizeIsOwnerAsync(engine))
			return Forbid();

		await _engineService.CancelBuildAsync(id);
		return Ok();
	}

	/// <summary>
	/// Upload a data file for use by the specified engine.
	/// </summary>
	/// <param name="id">The engine id.</param>
	/// <param name="dataType">The type of data.</param>
	/// <param name="name">The label for the file.</param>
	/// <param name="format">The file format.</param>
	/// <param name="corpusType">The parallel corpus type of the data file.</param>
	/// <param name="corpusKey">The parallel corpus key that is used to associate the source and target data files.</param>
	/// <param name="file">The data file.</param>
	/// <response code="200">The data file was uploaded successfully.</response>
	[Authorize(Scopes.UpdateEngines)]
	[HttpPost("{id}/files")]
	[RequestSizeLimit(100_000_000)]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public async Task<ActionResult<DataFileDto>> UploadDataFileAsync(string id,
		[BindRequired][FromForm] DataType dataType, [BindRequired][FromForm] string name,
		[BindRequired][FromForm] FileFormat format, [FromForm] CorpusType? corpusType, [FromForm] string? corpusKey,
		[BindRequired] IFormFile file)
	{
		Engine? engine = await _engines.GetAsync(id);
		if (engine == null)
			return NotFound();
		if (!await AuthorizeIsOwnerAsync(engine))
			return Forbid();

		var dataFile = new DataFile
		{
			EngineRef = id,
			DataType = dataType,
			Name = name,
			Format = format,
			CorpusType = corpusType,
			CorpusKey = corpusKey
		};
		using (Stream stream = file.OpenReadStream())
		{
			await _dataFileService.CreateAsync(dataFile, stream);
		}
		DataFileDto dto = CreateDto(dataFile);
		return Created(dto.Href, dto);
	}

	/// <summary>
	/// Gets all data files for the specified engine.
	/// </summary>
	/// <param name="id">The engine id.</param>
	/// <response code="200">The data files.</response>
	[Authorize(Scopes.ReadEngines)]
	[HttpGet("{id}/files")]
	public async Task<ActionResult<IEnumerable<DataFileDto>>> GetAllDataFilesAsync(string id)
	{
		Engine? engine = await _engines.GetAsync(id);
		if (engine == null)
			return NotFound();
		if (!await AuthorizeIsOwnerAsync(engine))
			return Forbid();

		return Ok((await _dataFiles.GetAllAsync()).Select(CreateDto));
	}

	/// <summary>
	/// Gets the specified data file.
	/// </summary>
	/// <param name="id">The engine id.</param>
	/// <param name="fileId">The data file id.</param>
	/// <response code="200">The data file.</response>
	[Authorize(Scopes.ReadEngines)]
	[HttpGet("{id}/files/{fileId}")]
	public async Task<ActionResult<DataFileDto>> GetDataFileAsync(string id, string fileId)
	{
		Engine? engine = await _engines.GetAsync(id);
		if (engine == null)
			return NotFound();
		if (!await AuthorizeIsOwnerAsync(engine))
			return Forbid();

		DataFile? dataFile = await _dataFiles.GetAsync(fileId);
		if (dataFile == null)
			return NotFound();

		return Ok(CreateDto(dataFile));
	}

	/// <summary>
	/// Deletes the specified data file.
	/// </summary>
	/// <param name="id">The engine id.</param>
	/// <param name="fileId">The data file id.</param>
	/// <response code="200">The data file was deleted successfully.</response>
	[Authorize(Scopes.UpdateEngines)]
	[HttpDelete("{id}/files/{fileId}")]
	[ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
	public async Task<ActionResult> DeleteDataFileAsync(string id, string fileId)
	{
		Engine? engine = await _engines.GetAsync(id);
		if (engine == null)
			return NotFound();
		if (!await AuthorizeIsOwnerAsync(engine))
			return Forbid();

		if (!await _dataFileService.DeleteAsync(fileId))
			return NotFound();

		return Ok();
	}

	/// <summary>
	/// Deletes all data files for the specified engine.
	/// </summary>
	/// <param name="id">The engine id.</param>
	/// <response code="200">All data files were deleted successfully.</response>
	[Authorize(Scopes.UpdateEngines)]
	[HttpDelete("{id}/files")]
	[ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
	public async Task<ActionResult> DeleteAllDataFilesAsync(string id)
	{
		Engine? engine = await _engines.GetAsync(id);
		if (engine == null)
			return NotFound();
		if (!await AuthorizeIsOwnerAsync(engine))
			return Forbid();

		await _dataFileService.DeleteAllByEngineIdAsync(id);
		return Ok();
	}

	private async Task<bool> AuthorizeIsOwnerAsync(Engine engine)
	{
		AuthorizationResult result = await _authService.AuthorizeAsync(User, engine, "IsOwner");
		return result.Succeeded;
	}

	private static TranslationResultDto? CreateDto(TranslationResult result)
	{
		if (result == null)
			return null;

		return new TranslationResultDto
		{
			Target = result.TargetSegment.ToArray(),
			Confidences = result.WordConfidences.Select(c => (float)c).ToArray(),
			Sources = result.WordSources.ToArray(),
			Alignment = CreateDto(result.Alignment),
			Phrases = result.Phrases.Select(CreateDto).ToArray()
		};
	}

	private static WordGraphDto CreateDto(WordGraph wordGraph)
	{
		return new WordGraphDto
		{
			InitialStateScore = (float)wordGraph.InitialStateScore,
			FinalStates = wordGraph.FinalStates.ToArray(),
			Arcs = wordGraph.Arcs.Select(CreateDto).ToArray()
		};
	}

	private static WordGraphArcDto CreateDto(WordGraphArc arc)
	{
		return new WordGraphArcDto
		{
			PrevState = arc.PrevState,
			NextState = arc.NextState,
			Score = (float)arc.Score,
			Words = arc.Words.ToArray(),
			Confidences = arc.WordConfidences.Select(c => (float)c).ToArray(),
			SourceSegmentRange = CreateDto(arc.SourceSegmentRange),
			Sources = arc.WordSources.ToArray(),
			Alignment = CreateDto(arc.Alignment)
		};
	}

	private static AlignedWordPairDto[] CreateDto(WordAlignmentMatrix matrix)
	{
		var wordPairs = new List<AlignedWordPairDto>();
		for (int i = 0; i < matrix.RowCount; i++)
		{
			for (int j = 0; j < matrix.ColumnCount; j++)
			{
				if (matrix[i, j])
					wordPairs.Add(new AlignedWordPairDto { SourceIndex = i, TargetIndex = j });
			}
		}
		return wordPairs.ToArray();
	}

	private EngineDto CreateDto(Engine engine)
	{
		return new EngineDto
		{
			Id = engine.Id,
			Href = Url.RouteUrl(RouteNames.Engines) + $"/{engine.Id}",
			SourceLanguageTag = engine.SourceLanguageTag,
			TargetLanguageTag = engine.TargetLanguageTag,
			Type = engine.Type,
			Confidence = engine.Confidence,
			TrainedSegmentCount = engine.TrainedSegmentCount
		};
	}

	private static RangeDto CreateDto(Range<int> range)
	{
		return new RangeDto()
		{
			Start = range.Start,
			End = range.End
		};
	}

	private static PhraseDto CreateDto(Phrase phrase)
	{
		return new PhraseDto
		{
			SourceSegmentRange = CreateDto(phrase.SourceSegmentRange),
			TargetSegmentCut = phrase.TargetSegmentCut,
			Confidence = phrase.Confidence
		};
	}

	private BuildDto CreateDto(Build build)
	{
		return new BuildDto
		{
			Id = build.Id,
			Href = Url.RouteUrl(RouteNames.Engines) + $"/builds/{build.Id}",
			Revision = build.Revision,
			Engine = new ResourceDto
			{
				Id = build.EngineRef,
				Href = Url.RouteUrl(RouteNames.Engines) + $"/{build.EngineRef}"
			},
			PercentCompleted = build.PercentCompleted,
			Message = build.Message,
			State = build.State
		};
	}

	private DataFileDto CreateDto(DataFile dataFile)
	{
		return new DataFileDto
		{
			Id = dataFile.Id,
			Href = Url.RouteUrl(RouteNames.Engines) + $"/files/{dataFile.Id}",
			Engine = new ResourceDto
			{
				Id = dataFile.EngineRef,
				Href = Url.RouteUrl(RouteNames.Engines) + $"/{dataFile.EngineRef}"
			},
			DataType = dataFile.DataType,
			Name = dataFile.Name,
			Format = dataFile.Format,
			CorpusType = dataFile.CorpusType,
			CorpusKey = dataFile.CorpusKey
		};
	}
}
