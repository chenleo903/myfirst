import { useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Table,
  Card,
  Input,
  Select,
  Button,
  Space,
  Tag,
  message,
  Popconfirm,
  Row,
  Col,
} from 'antd';
import {
  PlusOutlined,
  SearchOutlined,
  EditOutlined,
  DeleteOutlined,
  EyeOutlined,
} from '@ant-design/icons';
import type { TablePaginationConfig, SorterResult } from 'antd/es/table/interface';
import { useCustomers, useDeleteCustomer } from '../hooks/useCustomers';
import type { Customer, CustomerSearchRequest, CustomerStatus, CustomerSource } from '../types';

const { Option } = Select;

// Status display configuration
const statusConfig: Record<CustomerStatus, { color: string; label: string }> = {
  Lead: { color: 'blue', label: '线索' },
  Contacted: { color: 'cyan', label: '已联系' },
  NeedsAnalyzed: { color: 'orange', label: '待分析' },
  Quoted: { color: 'purple', label: '已报价' },
  Negotiating: { color: 'gold', label: '谈判中' },
  Won: { color: 'green', label: '成交' },
  Lost: { color: 'red', label: '流失' },
};

// Source display configuration
const sourceConfig: Record<CustomerSource, string> = {
  Website: '网站',
  Referral: '推荐',
  SocialMedia: '社交媒体',
  Event: '活动',
  DirectContact: '直接联系',
  Other: '其他',
};

export default function CustomerListPage() {
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useState<CustomerSearchRequest>({
    page: 1,
    pageSize: 20,
    sortBy: 'LastInteractionAt',
    sortOrder: 'desc',
  });
  const [keyword, setKeyword] = useState('');

  const { data, isLoading, error } = useCustomers(searchParams);
  const deleteCustomer = useDeleteCustomer();

  const handleSearch = useCallback(() => {
    setSearchParams((prev) => ({
      ...prev,
      keyword: keyword || undefined,
      page: 1,
    }));
  }, [keyword]);


  const handleStatusFilter = useCallback((value: CustomerStatus | undefined) => {
    setSearchParams((prev) => ({
      ...prev,
      status: value,
      page: 1,
    }));
  }, []);

  const handleSourceFilter = useCallback((value: CustomerSource | undefined) => {
    setSearchParams((prev) => ({
      ...prev,
      source: value,
      page: 1,
    }));
  }, []);

  const handleIndustryFilter = useCallback((value: string | undefined) => {
    setSearchParams((prev) => ({
      ...prev,
      industry: value,
      page: 1,
    }));
  }, []);

  const handleTableChange = useCallback(
    (
      pagination: TablePaginationConfig,
      _filters: Record<string, unknown>,
      sorter: SorterResult<Customer> | SorterResult<Customer>[]
    ) => {
      const singleSorter = Array.isArray(sorter) ? sorter[0] : sorter;
      const sortField = singleSorter.field as string | undefined;
      const sortOrder = singleSorter.order;

      let sortBy: CustomerSearchRequest['sortBy'] = undefined;
      if (sortField === 'lastInteractionAt') sortBy = 'LastInteractionAt';
      else if (sortField === 'createdAt') sortBy = 'CreatedAt';
      else if (sortField === 'updatedAt') sortBy = 'UpdatedAt';

      setSearchParams((prev) => ({
        ...prev,
        page: pagination.current || 1,
        pageSize: pagination.pageSize || 20,
        sortBy: sortBy || prev.sortBy,
        sortOrder: sortOrder === 'ascend' ? 'asc' : sortOrder === 'descend' ? 'desc' : prev.sortOrder,
      }));
    },
    []
  );

  const handleDelete = useCallback(
    async (id: string) => {
      try {
        await deleteCustomer.mutateAsync({ id });
        message.success('客户已删除');
      } catch {
        message.error('删除失败');
      }
    },
    [deleteCustomer]
  );

  const columns = [
    {
      title: '公司名称',
      dataIndex: 'companyName',
      key: 'companyName',
      width: 200,
      ellipsis: true,
    },
    {
      title: '联系人',
      dataIndex: 'contactName',
      key: 'contactName',
      width: 120,
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      width: 100,
      render: (status: CustomerStatus) => (
        <Tag color={statusConfig[status]?.color}>{statusConfig[status]?.label || status}</Tag>
      ),
    },
    {
      title: '行业',
      dataIndex: 'industry',
      key: 'industry',
      width: 120,
      ellipsis: true,
    },
    {
      title: '来源',
      dataIndex: 'source',
      key: 'source',
      width: 100,
      render: (source: CustomerSource) => sourceConfig[source] || source || '-',
    },
    {
      title: '评分',
      dataIndex: 'score',
      key: 'score',
      width: 80,
      render: (score: number) => <span>{score}</span>,
    },
    {
      title: '最后互动',
      dataIndex: 'lastInteractionAt',
      key: 'lastInteractionAt',
      width: 160,
      sorter: true,
      render: (date: string | undefined) =>
        date ? new Date(date).toLocaleString('zh-CN') : '-',
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 160,
      sorter: true,
      render: (date: string) => new Date(date).toLocaleString('zh-CN'),
    },
    {
      title: '操作',
      key: 'actions',
      width: 150,
      fixed: 'right' as const,
      render: (_: unknown, record: Customer) => (
        <Space size="small">
          <Button
            type="link"
            size="small"
            icon={<EyeOutlined />}
            onClick={() => navigate(`/customers/${record.id}`)}
          />
          <Button
            type="link"
            size="small"
            icon={<EditOutlined />}
            onClick={() => navigate(`/customers/${record.id}/edit`)}
          />
          <Popconfirm
            title="确定要删除此客户吗？"
            onConfirm={() => handleDelete(record.id)}
            okText="确定"
            cancelText="取消"
          >
            <Button type="link" size="small" danger icon={<DeleteOutlined />} />
          </Popconfirm>
        </Space>
      ),
    },
  ];


  if (error) {
    return (
      <Card>
        <div style={{ textAlign: 'center', padding: '40px' }}>
          <p>加载失败，请稍后重试</p>
        </div>
      </Card>
    );
  }

  const customers = data?.data?.items || [];
  const total = data?.data?.total || 0;

  return (
    <div style={{ padding: '24px' }}>
      <Card
        title="客户列表"
        extra={
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => navigate('/customers/new')}
          >
            新建客户
          </Button>
        }
      >
        {/* Filters */}
        <Row gutter={[16, 16]} style={{ marginBottom: 16 }}>
          <Col xs={24} sm={12} md={6}>
            <Input
              placeholder="搜索公司名称或联系人"
              prefix={<SearchOutlined />}
              value={keyword}
              onChange={(e) => setKeyword(e.target.value)}
              onPressEnter={handleSearch}
              allowClear
            />
          </Col>
          <Col xs={24} sm={12} md={4}>
            <Select
              placeholder="状态筛选"
              allowClear
              style={{ width: '100%' }}
              value={searchParams.status}
              onChange={handleStatusFilter}
            >
              {Object.entries(statusConfig).map(([key, { label }]) => (
                <Option key={key} value={key}>
                  {label}
                </Option>
              ))}
            </Select>
          </Col>
          <Col xs={24} sm={12} md={4}>
            <Select
              placeholder="来源筛选"
              allowClear
              style={{ width: '100%' }}
              value={searchParams.source}
              onChange={handleSourceFilter}
            >
              {Object.entries(sourceConfig).map(([key, label]) => (
                <Option key={key} value={key}>
                  {label}
                </Option>
              ))}
            </Select>
          </Col>
          <Col xs={24} sm={12} md={4}>
            <Input
              placeholder="行业筛选"
              value={searchParams.industry || ''}
              onChange={(e) => handleIndustryFilter(e.target.value || undefined)}
              allowClear
            />
          </Col>
          <Col xs={24} sm={12} md={2}>
            <Button type="primary" onClick={handleSearch}>
              搜索
            </Button>
          </Col>
        </Row>

        {/* Table */}
        <Table
          columns={columns}
          dataSource={customers}
          rowKey="id"
          loading={isLoading}
          scroll={{ x: 1200 }}
          pagination={{
            current: searchParams.page,
            pageSize: searchParams.pageSize,
            total,
            showSizeChanger: true,
            showQuickJumper: true,
            showTotal: (total) => `共 ${total} 条`,
            pageSizeOptions: ['10', '20', '50', '100'],
          }}
          onChange={handleTableChange}
        />
      </Card>
    </div>
  );
}
