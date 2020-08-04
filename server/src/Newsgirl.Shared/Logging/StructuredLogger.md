
# The problem

Standard logging frameworks like `log4net`, `serilog` and `NLog` have several problems:

* Lack of first class support for structured logging.

* Log message granularity is centered around arbitrary "levels" like `Debug`, `Warning` and `Error`.

* Performance. When using the aforementioned frameworks, blocking work is being done when logging a message. 

# Goals of this module

* To provide an extensible framework for structured logging.

* Allow different event streams to be turned on and off and assigned different destinations at runtime. When an event stream is turned off there should be minimal performance penalty for emitting an event to that stream.

* To allow events to have static types.
