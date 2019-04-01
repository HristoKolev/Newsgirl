import React from 'react';
import {Link} from 'react-router-dom';
import {Card, CardBody, CardHeader, Table, TableBody, TableHead} from 'mdbreact';

import {AppContext} from '../infrastructure/context';
import {Field, Form, Formik} from 'formik';
import { BaseComponent } from '../infrastructure/components/BaseComponent';
import { SearchFeedsRequest, FeedDto, SearchFeedsResponse } from '../dto';
import { fields } from '../infrastructure/fields/fields';
import LoadingSpinner from '../infrastructure/components/LoadingSpinner';

interface Props {
  context: AppContext;
}

interface State {
  filter: SearchFeedsRequest;
  listItems: FeedDto[];
  loading: boolean;
}

export class FeedListComponent extends BaseComponent<Props, State> {

  state: State = {
    loading: false,
    listItems: [],
    filter: {
      query: '',
    },
  };

  async searchItems() {

    const {api, allActions} = this.props.context;

    await this.setStateAsync({
      loading: true,
      listItems: [],
    });

    const response = await api.send<SearchFeedsRequest, SearchFeedsResponse>(
      'SearchFeedsRequest',
      this.state.filter,
    );

    if (response.success) {
      await this.setStateAsync({
        loading: false,
        listItems: response.payload.items,
      });
    } else {

      allActions.errors.setErrors(response.errorMessages);

      await this.setStateAsync({
        loading: false,
      });
    }
  }

  componentDidMountAsync = async () => {
    await this.searchItems();
  };

  submit = async (data: any) => {
    await this.setStateAsync({filter: data});
    await this.searchItems();
  };

  render() {
    const {listItems, loading} = this.state;
    return (
      <Formik initialValues={this.state.filter}
              onSubmit={this.unwrapPromise(this.submit)}
              validate={this.unwrapPromise(this.submit)}>
        <Form>
          <Card>
            <CardHeader color="red">Feeds</CardHeader>
            <CardBody>
              <div className="row">

                <div className="col-md-6">
                  <Field
                    component={fields.TextField}
                    name="query"
                    label="Search"
                  />
                </div>

                <div className="col-md-3 vertical-center">
                  <button
                    className="btn btn-md btn-default Ripple-parent"
                    type="submit"
                    style={{
                      width: '100%',
                    }}>
                    Refresh
                  </button>
                </div>

                <div className="col-md-3 vertical-center">
                  <Link
                    to={`/feeds/new`}
                    className="btn btn-md btn-success Ripple-parent"
                    style={{width: '100%'}}>
                    New feed
                  </Link>
                </div>
              </div>

              <Table responsive style={{marginTop: '1rem'}} className="list-table">
                <TableHead small="" color="red" textWhite>
                  <tr>
                    <th>#</th>
                    <th>Name</th>
                    <th/>
                    <th/>
                    <th/>
                  </tr>
                </TableHead>
                {listItems.length > 0 && <TableBody>
                  {listItems.map((item, postIndex) =>
                    <tr key={postIndex}>
                      <td>#{item.feedID}</td>
                      <td>{item.feedName}</td>
                      <td>
                        <Link
                          to={`/feeds/${item.feedID}`}
                          className="btn btn-sm btn-default Ripple-parent">
                          Show
                        </Link>
                      </td>

                      <td>
                        <Link
                          to={`/feeds/${item.feedID}/edit`}
                          className="btn btn-sm btn-warning Ripple-parent">
                          Edit
                        </Link>
                      </td>
                      <td>
                        <Link
                          to={`/feeds/${item.feedID}/delete`}
                          className="btn btn-sm btn-danger Ripple-parent">
                          Delete
                        </Link>
                      </td>
                    </tr>,
                  )}
                </TableBody>}
              </Table>

              {loading && <LoadingSpinner>Loading...</LoadingSpinner>}

            </CardBody>
          </Card>
        </Form>
      </Formik>
    );
  }
}
