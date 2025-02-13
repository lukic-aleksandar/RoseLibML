#!/bin/bash

SourceDir="./JoshClose-CsvHelper-852bd46";
DestDir="../JoshClose-CsvHelper-852bd46-cs";

cd ${SourceDir}
find . -type f -name "*.cs" |
while read x
do
  bn=`basename $x`;
  if [ -f "${DestDir}/$bn" ]
  then
    for i in {1..9999}
    do
        if [ ! -f "${DestDir}/${bn%.*}_${i}.${bn##*.}" ]
        then
            echo "Next free file extension is no $i";
            bn="${DestDir}/${bn%.*}_${i}.${bn##*.}"
            break;
        fi
    done
  fi
  echo "copy file $x to ${DestDir}/$bn";
  cp -p "$x" "${DestDir}/$bn";
 done