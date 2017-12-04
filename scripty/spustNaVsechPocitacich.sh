#!/bin/bash

for i in `seq 0 14`; do
	ssh u1-$i "./spustVic.sh u1-$i"
done
for i in `seq 0 25`; do
	ssh u2-$i "./spustVic.sh u2-$i"
done
