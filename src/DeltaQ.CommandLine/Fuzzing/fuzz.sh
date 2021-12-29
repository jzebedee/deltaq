afl-fuzz -i ../../test/assets -o findings -t 5000 -m 10000 -- dotnet bin/Release/net6.0/dq.dll fuzz
