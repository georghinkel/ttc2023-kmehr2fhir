expected = read.csv2("../expected-results/results.csv", sep=";")
actual = subset(read.csv2("../output/output.csv", sep=";"), MetricName=="Problems")

tools = unique(actual$Tool)

for (tool in tools) {
  tool_data = subset(actual, Tool==tool)
  mutant_sets = unique(tool_data$MutantSet)
  for (mutant_set in mutant_sets) {
    mset_data = subset(tool_data, MutantSet==mutant_set)
    sources = unique(mset_data$Source)
    for (source in sources) {
        source_data = subset(mset_data, Source==source)
        source_n = length(row.names(source_data))

        for (i in 1:source_n) {
            query.row = source_data[i,]
            expected.row = subset(expected, MutantSet==query.row$MutantSet & Source==query.row$Source & Mutant==query.row$Mutant)

            if (length(as.character(expected.row$MetricValue)) > 0) {
               if (as.character(query.row$MetricValue) != as.character(expected.row$MetricValue)) {
                  print(paste(tool, "is wrong. Was ", query.row$MetricValue, "but expected", expected.row$MetricValue, "for mutant set", mutant_set, "source", source, "mutant", query.row$Mutant, "run", query.row$RunIndex))
               }
            } else {
              print(paste("Warning:", tool, "produced the result", query.row$MetricValue, "but expected result is unavailable for mutant set", mutant_set, "source", source, "mutant", query.row$Mutant, "run", query.row$RunIndex))
            }

        }
    }

  }
}
