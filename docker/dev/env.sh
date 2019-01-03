#!/usr/bin/env bash

base_path=$(readlink -f $(pwd)/../../)

export stack_name="newsgirl-dev"

export backend_src=$base_path/server
export frontend_src=$base_path/client

export db_username="newsgirl"
export db_password="test123"
