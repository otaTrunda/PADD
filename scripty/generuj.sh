#!/bin/bash

while [ -t ]; do
	mono shuffler.exe > $1.in
	nice -n 19 ~/optimqtmSrc/optiqtm < $1.in | tee -a $1.out
done
