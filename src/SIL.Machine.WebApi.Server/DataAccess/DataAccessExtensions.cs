﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NoDb;
using SIL.Machine.WebApi.Server.Models;
using SIL.Machine.WebApi.Server.Options;
using SIL.Threading;

namespace SIL.Machine.WebApi.Server.DataAccess
{
	public enum EngineLocatorType
	{
		Id,
		LanguageTag,
		Project
	}

	public enum BuildLocatorType
	{
		Id,
		Engine
	}

	public static class DataAccessExtensions
	{
		public static async Task<Engine> GetByLocatorAsync(this IEngineRepository engineRepo, EngineLocatorType locatorType,
			string locator)
		{
			switch (locatorType)
			{
				case EngineLocatorType.Id:
					return await engineRepo.GetAsync(locator);
				case EngineLocatorType.LanguageTag:
					int index = locator.IndexOf("_", StringComparison.OrdinalIgnoreCase);
					string sourceLanguageTag = locator.Substring(0, index);
					string targetLanguageTag = locator.Substring(index + 1);
					return await engineRepo.GetByLanguageTagAsync(sourceLanguageTag, targetLanguageTag);
				case EngineLocatorType.Project:
					return await engineRepo.GetByProjectIdAsync(locator);
			}
			return null;
		}

		public static async Task<Build> GetByLocatorAsync(this IBuildRepository buildRepo, BuildLocatorType locatorType,
			string locator)
		{
			switch (locatorType)
			{
				case BuildLocatorType.Id:
					return await buildRepo.GetAsync(locator);
				case BuildLocatorType.Engine:
					return await buildRepo.GetByEngineIdAsync(locator);
			}
			return null;
		}

		public static async Task<T> ConcurrentUpdateAsync<T>(this IRepository<T> repo, T entity, Action<T> changeAction)
			where T : class, IEntity<T>
		{
			while (true)
			{
				try
				{
					changeAction(entity);
					await repo.UpdateAsync(entity, true);
					break;
				}
				catch (ConcurrencyConflictException)
				{
					entity = await repo.GetAsync(entity.Id);
					if (entity == null)
						return null;
				}
			}
			return entity;
		}

		public static bool TryConcurrentUpdate<T>(this IRepository<T> repo, T entity, Action<T> changeAction, out T newEntity)
			where T : class, IEntity<T>
		{
			while (true)
			{
				try
				{
					changeAction(entity);
					repo.Update(entity, true);
					break;
				}
				catch (ConcurrencyConflictException)
				{
					if (!repo.TryGet(entity.Id, out entity))
					{
						newEntity = null;
						return false;
					}
				}
			}
			newEntity = entity;
			return true;
		}

		public static Task<T> GetNewerRevisionAsync<T>(this IRepository<T> repo, string id, long minRevision)
			where T : class, IEntity<T>
		{
			return GetNewerRevisionAsync(repo.SubscribeAsync, repo.GetAsync, id, minRevision);
		}

		public static Task<Build> GetNewerRevisionByEngineIdAsync(this IBuildRepository repo, string engineId, long minRevision)
		{
			return GetNewerRevisionAsync(repo.SubscribeByEngineIdAsync, repo.GetByEngineIdAsync, engineId, minRevision);
		}

		public static Task<Build> GetNewerRevisionAsync(this IBuildRepository repo, BuildLocatorType locatorType, string locator,
			long minRevision)
		{
			switch (locatorType)
			{
				case BuildLocatorType.Id:
					return repo.GetNewerRevisionAsync(locator, minRevision);
				case BuildLocatorType.Engine:
					return repo.GetNewerRevisionByEngineIdAsync(locator, minRevision);
			}
			return null;
		}

		private static async Task<TEntity> GetNewerRevisionAsync<TKey, TEntity>(
			Func<TKey, Action<EntityChange<TEntity>>, Task<IDisposable>> subscribe, Func<TKey, Task<TEntity>> getEntity, TKey key,
			long minRevision) where TEntity : class, IEntity<TEntity>
		{
			var changeEvent = new AsyncAutoResetEvent();
			TEntity entity;
			var change = new EntityChange<TEntity>();
			void HandleChange(EntityChange<TEntity> c)
			{
				change = c;
				changeEvent.Set();
			}
			using (await subscribe(key, HandleChange))
			{
				entity = await getEntity(key);
				while (entity == null || minRevision > entity.Revision)
				{
					await changeEvent.WaitAsync();
					if (change.Type == EntityChangeType.Delete)
						return null;
					entity = change.Entity;
				}
			}
			return entity;
		}

		public static IServiceCollection AddNoDbDataAccess(this IServiceCollection services, IConfigurationRoot config)
		{
			services.Configure<NoDbDataAccessOptions>(config.GetSection("NoDbDataAccess"));
			services.AddNoDbForEntity<Engine>();
			services.AddNoDbForEntity<Build>();
			services.AddSingleton<IEngineRepository>(sp => new MemoryEngineRepository(
				new NoDbEngineRepository(sp.GetService<IBasicCommands<Engine>>(), sp.GetService<IBasicQueries<Engine>>())));
			services.AddSingleton<IBuildRepository>(sp => new MemoryBuildRepository(
				new NoDbBuildRepository(sp.GetService<IBasicCommands<Build>>(), sp.GetService<IBasicQueries<Build>>())));
			return services;
		}

		private static void AddNoDbForEntity<T>(this IServiceCollection services) where T : class
		{
			services.AddSingleton<IBasicCommands<T>, BasicCommands<T>>();
			services.AddSingleton<IBasicQueries<T>, BasicQueries<T>>();
			services.AddSingleton<IStringSerializer<T>, StringSerializer<T>>();
			services.AddSingleton<IStoragePathResolver<T>, MachineStoragePathResolver<T>>();
		}

		public static IServiceCollection AddMemoryDataAccess(this IServiceCollection services)
		{
			services.AddSingleton<IEngineRepository, MemoryEngineRepository>();
			services.AddSingleton<IBuildRepository, MemoryBuildRepository>();
			return services;
		}
	}
}
