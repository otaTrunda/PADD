#!/bin/bash

cd runTests

cd Release

ulimit -t unlimited

screen -d -m mono PADD.exe $1