﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Utils
{
	public class PhasedProgressReporter
	{
		private readonly IProgress<ProgressStatus> _progress;
		private int _currentPhaseIndex = -1;
		private double _defaultPercentage;
		private double _percentCompleted;

		public PhasedProgressReporter(IProgress<ProgressStatus> progress, params Phase[] phases)
		{
			_progress = progress;
			Phases = phases;

			double sum = 0;
			int unspecifiedCount = 0;
			foreach (Phase phase in Phases)
			{
				sum += phase.Percentage;
				if (phase.Percentage == 0)
					unspecifiedCount++;
			}

			_defaultPercentage = unspecifiedCount == 0 ? 0 : (1.0 - sum) / unspecifiedCount;
		}

		public PhasedProgressReporter(IProgress<ProgressStatus> progress, IEnumerable<Phase> phases)
			: this(progress, phases.ToArray())
		{
		}

		public IReadOnlyList<Phase> Phases { get; }

		public Phase CurrentPhase => _currentPhaseIndex == -1 ? null : Phases[_currentPhaseIndex];

		private double CurrentPhasePercentage
		{
			get
			{
				if (_currentPhaseIndex == -1)
					return 0;
				double pcnt = CurrentPhase.Percentage;
				if (pcnt == 0)
					pcnt = _defaultPercentage;
				return pcnt;
			}
		}

		public virtual PhaseProgress StartNextPhase()
		{
			_percentCompleted += CurrentPhasePercentage;
			_currentPhaseIndex++;

			return new PhaseProgress(this, Phases[_currentPhaseIndex]);
		}

		protected internal virtual void Report(ProgressStatus value)
		{
			if (_progress == null)
				return;

			double percentCompleted = _percentCompleted + CurrentPhasePercentage * value.PercentCompleted;
			string message = value.Message ?? Phases[_currentPhaseIndex].Message;
			_progress.Report(new ProgressStatus(percentCompleted, message));
		}
	}
}
