﻿using System.Collections.Generic;
using SIL.Collections;

namespace SIL.Machine.Rules
{
	public class RuleBatch<TData, TOffset> : IRule<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly List<IRule<TData, TOffset>> _rules;
		private readonly bool _disjunctive;
		private readonly IEqualityComparer<TData> _comparer; 

		public RuleBatch(IEnumerable<IRule<TData, TOffset>> rules)
			: this(rules, EqualityComparer<TData>.Default)
		{
		}

		public RuleBatch(IEnumerable<IRule<TData, TOffset>> rules, IEqualityComparer<TData> comparer)
			: this(rules, true, comparer)
		{
		}

		public RuleBatch(IEnumerable<IRule<TData, TOffset>> rules, bool disjunctive)
			: this(rules, disjunctive, EqualityComparer<TData>.Default)
		{
		}

		public RuleBatch(IEnumerable<IRule<TData, TOffset>> rules, bool disjunctive, IEqualityComparer<TData> comparer)
		{
			_rules = new List<IRule<TData, TOffset>>(rules);
			_disjunctive = disjunctive;
			_comparer = comparer;
		}

		public IReadOnlyList<IRule<TData, TOffset>> Rules
		{
			get { return _rules.AsReadOnlyList(); }
		}

		public virtual bool IsApplicable(TData input)
		{
			return true;
		}

		public virtual IEnumerable<TData> Apply(TData input)
		{
			var output = new HashSet<TData>(_comparer);
			foreach (IRule<TData, TOffset> rule in _rules)
			{
				if (rule.IsApplicable(input))
				{
					output.UnionWith(rule.Apply(input));
					if (_disjunctive && output.Count > 0)
						return output;
				}
			}

			return output;
		}
	}
}
