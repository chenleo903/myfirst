import { useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Card, Form, Input, Select, InputNumber, Button, Space, Spin, message } from 'antd';
import { ArrowLeftOutlined, SaveOutlined } from '@ant-design/icons';
import { useCustomer, useCreateCustomer, useUpdateCustomer } from '../hooks/useCustomers';
import type { CreateCustomerRequest, CustomerStatus, CustomerSource } from '../types';

const { Option } = Select;

// Status options
const statusOptions: { value: CustomerStatus; label: string }[] = [
  { value: 'Lead', label: '线索' },
  { value: 'Contacted', label: '已联系' },
  { value: 'NeedsAnalyzed', label: '待分析' },
  { value: 'Quoted', label: '已报价' },
  { value: 'Negotiating', label: '谈判中' },
  { value: 'Won', label: '成交' },
  { value: 'Lost', label: '流失' },
];

// Source options
const sourceOptions: { value: CustomerSource; label: string }[] = [
  { value: 'Website', label: '网站' },
  { value: 'Referral', label: '推荐' },
  { value: 'SocialMedia', label: '社交媒体' },
  { value: 'Event', label: '活动' },
  { value: 'DirectContact', label: '直接联系' },
  { value: 'Other', label: '其他' },
];

export default function CustomerFormPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [form] = Form.useForm<CreateCustomerRequest>();
  const isEditing = !!id;

  const { data: customerData, isLoading: customerLoading } = useCustomer(id || '', {
    enabled: isEditing,
  });
  const createCustomer = useCreateCustomer();
  const updateCustomer = useUpdateCustomer();

  const customer = customerData?.data?.data;
  const etag = customerData?.etag;


  // Populate form when editing
  useEffect(() => {
    if (customer) {
      form.setFieldsValue({
        companyName: customer.companyName,
        contactName: customer.contactName,
        wechat: customer.wechat,
        phone: customer.phone,
        email: customer.email,
        industry: customer.industry,
        source: customer.source,
        status: customer.status,
        tags: customer.tags,
        score: customer.score,
      });
    }
  }, [customer, form]);

  const handleSubmit = async (values: CreateCustomerRequest) => {
    try {
      if (isEditing && id) {
        await updateCustomer.mutateAsync({
          id,
          request: values,
          etag,
        });
        message.success('客户信息已更新');
        navigate(`/customers/${id}`);
      } else {
        const result = await createCustomer.mutateAsync(values);
        message.success('客户创建成功');
        const newId = result.data?.data?.id;
        if (newId) {
          navigate(`/customers/${newId}`);
        } else {
          navigate('/customers');
        }
      }
    } catch (error) {
      const err = error as { response?: { data?: { errors?: { message: string }[] } } };
      const errorMessage = err.response?.data?.errors?.[0]?.message || '操作失败';
      message.error(errorMessage);
    }
  };

  if (isEditing && customerLoading) {
    return (
      <div style={{ padding: '24px', textAlign: 'center' }}>
        <Spin size="large" />
      </div>
    );
  }

  const isSubmitting = createCustomer.isPending || updateCustomer.isPending;


  return (
    <div style={{ padding: '24px' }}>
      <div style={{ marginBottom: 16 }}>
        <Button
          icon={<ArrowLeftOutlined />}
          onClick={() => navigate(isEditing ? `/customers/${id}` : '/customers')}
        >
          返回
        </Button>
      </div>

      <Card title={isEditing ? '编辑客户' : '新建客户'}>
        <Form
          form={form}
          layout="vertical"
          onFinish={handleSubmit}
          initialValues={{
            status: 'Lead',
            score: 0,
          }}
          style={{ maxWidth: 800 }}
        >
          <Form.Item
            name="companyName"
            label="公司名称"
            rules={[
              { required: true, message: '请输入公司名称' },
              { max: 200, message: '公司名称不能超过200个字符' },
            ]}
          >
            <Input placeholder="请输入公司名称" />
          </Form.Item>

          <Form.Item
            name="contactName"
            label="联系人姓名"
            rules={[
              { required: true, message: '请输入联系人姓名' },
              { max: 200, message: '联系人姓名不能超过200个字符' },
            ]}
          >
            <Input placeholder="请输入联系人姓名" />
          </Form.Item>

          <Form.Item
            name="phone"
            label="电话"
            rules={[{ max: 50, message: '电话不能超过50个字符' }]}
          >
            <Input placeholder="请输入电话号码" />
          </Form.Item>

          <Form.Item
            name="email"
            label="邮箱"
            rules={[
              { type: 'email', message: '请输入有效的邮箱地址' },
              { max: 255, message: '邮箱不能超过255个字符' },
            ]}
          >
            <Input placeholder="请输入邮箱地址" />
          </Form.Item>

          <Form.Item
            name="wechat"
            label="微信"
            rules={[{ max: 100, message: '微信号不能超过100个字符' }]}
          >
            <Input placeholder="请输入微信号" />
          </Form.Item>

          <Form.Item
            name="industry"
            label="行业"
            rules={[{ max: 100, message: '行业不能超过100个字符' }]}
          >
            <Input placeholder="请输入行业" />
          </Form.Item>

          <Form.Item name="source" label="来源">
            <Select placeholder="请选择来源" allowClear>
              {sourceOptions.map((opt) => (
                <Option key={opt.value} value={opt.value}>
                  {opt.label}
                </Option>
              ))}
            </Select>
          </Form.Item>

          <Form.Item name="status" label="状态" rules={[{ required: true, message: '请选择状态' }]}>
            <Select placeholder="请选择状态">
              {statusOptions.map((opt) => (
                <Option key={opt.value} value={opt.value}>
                  {opt.label}
                </Option>
              ))}
            </Select>
          </Form.Item>

          <Form.Item
            name="score"
            label="评分"
            rules={[
              { type: 'number', min: 0, max: 100, message: '评分必须在0-100之间' },
            ]}
          >
            <InputNumber min={0} max={100} style={{ width: '100%' }} placeholder="请输入评分 (0-100)" />
          </Form.Item>

          <Form.Item
            name="tags"
            label="标签"
            tooltip="多个标签请用逗号分隔"
          >
            <Select
              mode="tags"
              placeholder="输入标签后按回车添加"
              tokenSeparators={[',']}
            />
          </Form.Item>

          <Form.Item>
            <Space>
              <Button
                type="primary"
                htmlType="submit"
                icon={<SaveOutlined />}
                loading={isSubmitting}
              >
                {isEditing ? '保存' : '创建'}
              </Button>
              <Button onClick={() => navigate(isEditing ? `/customers/${id}` : '/customers')}>
                取消
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
}
