#!/bin/bash
cd runTests
cd Release
ulimit -t unlimited
screen -d -m mono PADDHeapTest.exe $1