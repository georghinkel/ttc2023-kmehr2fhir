expected = read.csv2("../expected-results/results.csv", sep=";")
actual = subset(read.csv2("../output/output.csv", sep=";"), MetricName=="Entries")

tools = unique(actual$Tool)

for (tool in tools) {
  tool_data = subset(actual, Tool==tool)
  run_indices = unique(tool_data$RunIndex)
  for (run_index in run_indices) {
    ridx_data = subset(tool_data, RunIndex==run_index)
    sources = unique(ridx_data$Source)
    for (source in sources) {
        source_data = subset(ridx_data, Source==source)
        source_n = length(row.names(source_data))

        for (i in 1:source_n) {
            query.row = source_data[i,]
            expected.row = subset(expected, RunIndex==query.row$RunIndex & Source==query.row$Source)

            if (length(as.character(expected.row$MetricValue)) > 0) {
               if (as.character(query.row$MetricValue) != as.character(expected.row$MetricValue)) {
                  print(paste(tool, "is wrong. Was ", query.row$MetricValue, "but expected", expected.row$MetricValue, "for source", source, "run", query.row$RunIndex))
               }
            } else {
              print(paste("Warning:", tool, "produced the result", query.row$MetricValue, "but expected result is unavailable for source", source, "run", query.row$RunIndex))
            }

        }
    }

  }
}
