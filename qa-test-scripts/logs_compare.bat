echo +++ Diff Comparing %1 vs %2 Log Files +++
echo off
cd C:\ABS\TestClient\
diff -s --suppress-common-lines --minimal LogFolder\%1\TestRun-%1_no-ts.log LogFolder\%2\TestRun-%2_no-ts.log > diff_%1_vs_%2.log