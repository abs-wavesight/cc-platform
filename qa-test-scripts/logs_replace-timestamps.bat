echo +++ Masking out DateTime stamps in LogFile: LogFolder-%1-%2\TestRun-%1-%2.log +++
echo off
cd C:\ABS\TestClient\
echo %1-%2 > LogFolder\%1-%2\TestRun-%1-%2_no-ts.log
awk -F '+00:00' '{print "YYYY-MM-DD hh:mm:ss.sss +00:00" $2}' LogFolder-%1-%2\TestRun-%1-%2.log >> LogFolder\%1-%2\TestRun-%1-%2_no-ts.log
sed -i 's/\b[0-9]\{4\}-[0-9]\{2\}-[0-9]\{2\}T[0-9]\{2\}:[0-9]\{2\}:[0-9]\{2\}.[0-9]\{9\}Z/YYYY-MM-DDThh:mm:ss.sssssssss/g'  LogFolder\%1-%2\TestRun-%1-%2_no-ts.log
sed -i 's/\b[0-9]\{4\}-[0-9]\{2\}-[0-9]\{2\}T[0-9]\{2\}:[0-9]\{2\}:[0-9]\{2\}.[0-9]\{6\}Z/YYYY-MM-DDThh:mm:ss.sssssssss/g'  LogFolder\%1-%2\TestRun-%1-%2_no-ts.log