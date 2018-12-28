Dynamic recompilation core:

IL will have 256 registers, registers will be fetched/committed using specific fetch/commit opcodes.

Will be formed of chunks, which consist of only one entry point, but there might exist multiple exits.
Branch within chunks do not end the chunk.
Any forward branch ends a chunk (if-then heuristic?)

Memory handling...  Cache the pages?



=============================
WWR crashes - mixing code looks to be the culprit
FF4 crashes - coproc load/store
linux does not mount the fs succesfull