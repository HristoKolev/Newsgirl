
# Introduction

This module exposes an HTTP endpoint for RPC for processing requests.

# Parsing RPC requests

* RPC request type is read from the url: `/rpc/{RequestType}`. Example: `/rpc/PingRequest`. 

* The RPC request payload is read from the body of the HTTP request as JSON.

# Writing RPC responses

* The RPC result is serialized in JSON format and written as the body of the response.

# Future considerations

* Pool some objects like dictionaries to lower the GC pressure.
