

# Introduction

This module exposes an HTTP endpoint for RPC for processing requests.

# Parsing RPC requests

* RPC request type is read from the url: `/rpc/PingRequest`.
* The RPC request payload is read from the body of the HTTP request as JSON.
* RPC request headers consists of all HTTP headers whose names start with `rpc-`.

# Writing RPC responses

* The RPC result is serialized in JSON format and written as the body of the response.

* The RPC 
   