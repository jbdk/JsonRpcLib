# JsonRpcLib
C# DotNetCore 2.1+ Client/Server Json RPC library

Using Span&lt;T&gt;, Memory&lt;T&gt; and IO pipelines

[![Build Status](https://travis-ci.org/jbdk/JsonRpcLib.svg?branch=master)](https://travis-ci.org/jbdk/JsonRpcLib)


### Current performance 
Run the PerfTest app
 - 8 threads 1,000,000 json notify -> static class call: `1,250,000 requests/sec`
 - 8 threads 100,000 json invoke -> static class call: `35,000 requests/sec` 

Test machine: 3.4 Ghz i5 3570

As of 2018-05-04 I'm still investigating why the invoke request/sec is so much slower.

