using System.Collections.Generic;
using SIL.Machine;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.MorphologicalRules
{
	public class AnalysisAffixProcessRule : IRule<Word, ShapeNode>
	{
		private readonly Morpher _morpher;
		private readonly AffixProcessRule _rule;
		private readonly List<PatternRule<Word, ShapeNode>> _rules;

		public AnalysisAffixProcessRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, AffixProcessRule rule)
		{
			_morpher = morpher;
			_rule = rule;

			_rules = new List<PatternRule<Word, ShapeNode>>();
			foreach (AffixProcessAllomorph allo in rule.Allomorphs)
			{
				_rules.Add(new PatternRule<Word, ShapeNode>(spanFactory, new AnalysisAffixProcessAllomorphRuleSpec(allo), ApplicationMode.Multiple,
					new MatcherSettings<ShapeNode>
						{
							Filter = ann => ann.Type() == HCFeatureSystem.Segment,
							AnchoredToStart = true,
							AnchoredToEnd = true,
							AllSubmatches = true
						}));
			}
		}

		public bool IsApplicable(Word input)
		{
			return input.GetUnapplicationCount(_rule) < _rule.MaxApplicationCount
				&& _rule.OutSyntacticFeatureStruct.IsUnifiable(input.SyntacticFeatureStruct);
		}

		public IEnumerable<Word> Apply(Word input)
		{
			var output = new List<Word>();
			foreach (PatternRule<Word, ShapeNode> rule in _rules)
			{
				foreach (Word outWord in rule.Apply(input).RemoveDuplicates())
				{
					outWord.SyntacticFeatureStruct.Union(_rule.OutSyntacticFeatureStruct);
					outWord.SyntacticFeatureStruct.PriorityUnion(_rule.RequiredSyntacticFeatureStruct);
					outWord.MorphologicalRuleUnapplied(_rule);
					outWord.Freeze();
					if (_morpher.TraceRules.Contains(_rule))
					{
						var trace = new Trace(TraceType.MorphologicalRuleAnalysis, _rule) {Input = input, Output = outWord};
						outWord.CurrentTrace.Children.Add(trace);
						outWord.CurrentTrace = trace;
					}
					output.Add(outWord);
				}
			}
			if (output.Count == 0 && _morpher.TraceRules.Contains(_rule))
				input.CurrentTrace.Children.Add(new Trace(TraceType.MorphologicalRuleAnalysis, _rule) {Input = input});
			return output;
		}
	}
}
